using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MidiFile : MonoBehaviour
{



    // Note lengths
    //  We are working with 32 ticks to the crotchet. So
    //  all the other note lengths can be derived from this
    //  basic figure. Note that the longest note we can
    //  represent with this code is one tick short of a 
    //  two semibreves (i.e., 8 crotchets)

    public static readonly int SEMIQUAVER = 4;
    public static readonly int QUAVER = 8;
    public static readonly int CROTCHET = 16;
    public static readonly int MINIM = 32;
    public static readonly int SEMIBREVE = 64;

    // Standard MIDI file header, for one-track file
    // 4D, 54... are just magic numbers to identify the
    //  headers
    // Note that because we're only writing one track, we
    //  can for simplicity combine the file and track headers
    static readonly int[] header = new int[]
        {
            0x4d, 0x54, 0x68, 0x64, 0x00, 0x00, 0x00, 0x06,
            0x00, 0x00, // single-track format
            0x00, 0x01, // one track
            0x00, 0x10, // 16 ticks per quarter
            0x4d, 0x54, 0x72, 0x6B
        };

    // Standard footer
    static readonly int[] footer = new int[]
        {
            0x01, 0xFF, 0x2F, 0x00
        };

    // A MIDI event to set the tempo
    static readonly int[] tempoEvent = new int[]
        {
            0x00, 0xFF, 0x51, 0x03,
            0x0F, 0x42, 0x40 // Default 1 million usec per crotchet
        };

    // A MIDI event to set the key signature. This is irrelent to
    //  playback, but necessary for editing applications 
    static readonly int[] keySigEvent = new int[]
        {
            0x00, 0xFF, 0x59, 0x02,
            0x00, // C
            0x00  // major
        };


    // A MIDI event to set the time signature. This is irrelent to
    //  playback, but necessary for editing applications 
    static readonly int[] timeSigEvent = new int[]
        {
            0x00, 0xFF, 0x58, 0x04,
            0x04, // numerator
            0x02, // denominator (2==4, because it's a power of 2)
            0x30, // ticks per click (not used)
            0x08  // 32nd notes per crotchet 
        };

    // The collection of events to play, in time order
    protected List<int[]> playEvents;

    /** Construct a new MidiFile with an empty playback event list */
    public MidiFile()
    {
        playEvents = new List<int[]>();
    }

    /** Write the stored MIDI events to a file */
    public void writeToFile(string filename)
    {
        BinaryWriter fos = new BinaryWriter(File.Open(filename, FileMode.Create));

        fos.Write(intArrayToByteArray(header));

        // Calculate the amount of track data
        // _Do_ include the footer but _do not_ include the 
        // track header

        int size = tempoEvent.Length + keySigEvent.Length + timeSigEvent.Length + footer.Length;

        for (int i = 0; i < playEvents.Count; i++)
            size += playEvents[i].Length;

        // Write out the track data size in big-endian format
        // Note that this math is only valid for up to 64k of data
        //  (but that's a lot of notes) 
        int high = size / 256;
        int low = size - (high * 256);
        fos.Write((byte)0);
        fos.Write((byte)0);
        fos.Write((byte)high);
        fos.Write((byte)low);

        // Write the standard metadata — tempo, etc
        // At present, tempo is stuck at crotchet=60 
        fos.Write(intArrayToByteArray(tempoEvent));
        fos.Write(intArrayToByteArray(keySigEvent));
        fos.Write(intArrayToByteArray(timeSigEvent));

        // Write out the note, etc., events
        for (int i = 0; i < playEvents.Count; i++)
            fos.Write(intArrayToByteArray(playEvents[i]));

        // Write the footer and close
        fos.Write(intArrayToByteArray(footer));
        fos.Close();
    }


    /** Convert an array of integers which are assumed to contain
        unsigned bytes into an array of bytes */
    protected static byte[] intArrayToByteArray(int[] ints)
    {
        int l = ints.Length;
        byte[] outb = new byte[ints.Length];
        for (int i = 0; i < l; i++)
            outb[i] = (byte)ints[i];
        return outb;
    }


    /** Store a note-on event */
    public void noteOn(int delta, int note, int velocity)
    {
        int[] data = new int[4];
        data[0] = delta;
        data[1] = 0x90;
        data[2] = note;
        data[3] = velocity;
        playEvents.Add(data);
    }


    /** Store a note-off event */
    public void noteOff(int delta, int note)
    {
        int[] data = new int[4];
        data[0] = delta;
        data[1] = 0x80;
        data[2] = note;
        data[3] = 0;
        playEvents.Add(data);
    }


    /** Store a program-change event at current position */
    public void progChange(int prog)
    {
        int[] data = new int[3];
        data[0] = 0;
        data[1] = 0xC0;
        data[2] = prog;
        playEvents.Add(data);
    }


    /** Store a note-on event followed by a note-off event a note length
        later. There is no delta value — the note is assumed to
        follow the previous one with no gap. */
    public void noteOnOffNow(int duration, int note, int velocity)
    {
        noteOn(0, note, velocity);
        noteOff(duration, note);
    }


    public void noteSequenceFixedVelocity(int[] sequence, int velocity)
    {
        bool lastWasRest = false;
        int restDelta = 0;
        for (int i = 0; i < sequence.Length; i += 2)
        {
            int note = sequence[i];
            int duration = sequence[i + 1];
            if (note < 0)
            {
                // This is a rest
                restDelta += duration;
                lastWasRest = true;
            }
            else
            {
                // A note, not a rest
                if (lastWasRest)
                {
                    noteOn(restDelta, note, velocity);
                    noteOff(duration, note);
                }
                else
                {
                    noteOn(0, note, velocity);
                    noteOff(duration, note);
                }
                restDelta = 0;
                lastWasRest = false;
            }
        }
    }

    public void ExecuteTest()
    {

        MidiFile mf = new MidiFile();

        // Test 1 — play a C major chord

        // Turn on all three notes at start-of-track (delta=0) 
        mf.noteOn(0, 60, 127);
        mf.noteOn(0, 64, 127);
        mf.noteOn(0, 67, 127);

        // Turn off all three notes after one minim. 
        // NOTE delta value is cumulative — only _one_ of
        //  these note-offs has a non-zero delta. The second and
        //  third events are relative to the first
        mf.noteOff(MidiFile.MINIM, 60);
        mf.noteOff(0, 64);
        mf.noteOff(0, 67);

        // Test 2 — play a scale using noteOnOffNow
        //  We don't need any delta values here, so long as one
        //  note comes straight after the previous one 

        mf.noteOnOffNow(MidiFile.QUAVER, 60, 127);
        mf.noteOnOffNow(MidiFile.QUAVER, 62, 127);
        mf.noteOnOffNow(MidiFile.QUAVER, 64, 127);
        mf.noteOnOffNow(MidiFile.QUAVER, 65, 127);
        mf.noteOnOffNow(MidiFile.QUAVER, 67, 127);
        mf.noteOnOffNow(MidiFile.QUAVER, 69, 127);
        mf.noteOnOffNow(MidiFile.QUAVER, 71, 127);
        mf.noteOnOffNow(MidiFile.QUAVER, 72, 127);

        // Test 3 — play a short tune using noteSequenceFixedVelocity
        //  Note the rest inserted with a note value of -1

        int[] sequence = new int[]
        {
            60, MidiFile.QUAVER + MidiFile.SEMIQUAVER,
            65, MidiFile.SEMIQUAVER,
            70, MidiFile.CROTCHET + MidiFile.QUAVER,
            69, MidiFile.QUAVER,
            65, MidiFile.QUAVER / 3,
            62, MidiFile.QUAVER / 3,
            67, MidiFile.QUAVER / 3,
            72, MidiFile.MINIM + MidiFile.QUAVER,
            -1, MidiFile.SEMIQUAVER,
            72, MidiFile.SEMIQUAVER,
            76, MidiFile.MINIM,
        };

        // What the heck — use a different instrument for a change
        mf.progChange(10);

        mf.noteSequenceFixedVelocity(sequence, 127);

        mf.writeToFile("test1.mid");

    }


    public void SequenceNotes(int[] sequence)
    {

       // string path = "Assets/Resources/test.txt";

        MidiFile mf = new MidiFile();

        mf.progChange(10);

        mf.noteSequenceFixedVelocity(sequence, 127);

        // mf.writeToFile("Assets/StreamingAssets/Sound/Sequence.mp3");
        mf.writeToFile("Assets/Midi/Sequence.mp3");

        //mf.writeToFile("Assets/StreamingAssets/Sequence.wav");


    }
}
