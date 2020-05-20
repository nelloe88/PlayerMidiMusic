using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExeClass : MonoBehaviour
{
    public Button btnGera;
    public Button btnExec;
    public AudioSource audioSource;
    public AudioClip sequence;



    // Start is called before the first frame update
    void Start()
    {
        Button btn = btnGera.GetComponent<Button>();
        btnExec.onClick.AddListener(TaskOnClick);
        btn.onClick.AddListener(TaskOnClick);

    }

    // Update is called once per frame
    void Update()
    {
        

    }

    void TaskOnClick()
    {
        MidiFile exc = new MidiFile();

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

        try
        {
            //exc.ExecuteTest();

            exc.SequenceNotes(sequence);
            Debug.Log("Generade!!");
        }
        catch (Exception e)
        {
            Debug.Log("error:" + e);
        }
    }


}
