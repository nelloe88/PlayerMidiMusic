//
//  ValintaPlugin.m
//  ValintaPlugin
//
//  Created by Janne Aikioniemi on 25.9.2016.
//  Copyright © 2016 Zemeho Oy. All rights reserved.
//

// #import "ValintaPlugin.hpp"
#import <AdSupport/ASIdentifierManager.h>

extern "C"
{
    const char* GetIDFA()
    {
        ASIdentifierManager *manager = [ASIdentifierManager sharedManager];
        NSUUID *Idfa = manager.advertisingIdentifier;
        NSString *str = Idfa.UUIDString;
        const char *c = [str UTF8String];
        return c;
    }
    
}
