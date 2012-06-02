key savedID = NULL_KEY;
default
{
    state_entry()
    {
        // Set up the listener
        llListen(0, "", "", "");
    }
    state_exit()
    {
    }
    listen( integer channel, string name, key id, string message )
    {
        // Split the message by " " so that we can pull out commands
        list parsedMessage = llParseString2List(message,[" "], []);
        if(message == "leave")
        {
            // This allows the avatar to leave combat.
            // They cannot be killed, but they cannot hurt
            // anyone either. This removes them from the 
            // team they are on as well.
            aaLeaveCombat(id);
        }
        else if(message == "join")
        {
            // If the avatar left combat, this will re-add them into combat.
            // First, however, we must make sure they accept the permissions
            // for combat, otherwise they could run around and do things that
            // are not allowed in this combat situation.
            llRequestPermissions(id, PERMISSION_TAKE_CONTROLS);
            aaRequestCombatPermission(id);
            savedID = id;
        }
        else if(message == "get health")
        {
            // Tell the person directly with aaSayTo what their 
            // health is.
            aaSayTo(id, "Your health is: " + aaGetHealth(id));
        }
        else if(message == "get team")
        {
            // Tell the person directly with aaSayTo what their 
            // team is.
            aaSayTo(id, "Your team is: " + aaGetTeam(id));
        }
        else if(message == "get team members")
        {
            // Find the team members of the current team of the av.
            list teamMembers = aaGetTeamMembers(aaGetTeam(id));
            aaSayTo(id, "Your team is: " + aaGetTeam(id) + 
                    " and your team members are " + (string)teamMembers);
        }
        else if(llList2String(parsedMessage, 0) == "join" &&
                llList2String(parsedMessage, 1) == "team")
        {
            // Add the given user to the team that they requested to join.
            string team = llList2String(parsedMessage, 2);
            aaJoinCombatTeam(id, team);
        }
        else if(llList2String(parsedMessage, 0) == "freeze" &&
                llList2String(parsedMessage, 1) == "user")
        {
            // They want a user frozen by UUID
            string userToFreezeFirst = llList2String(parsedMessage, 2);
            string userToFreezeLast = llList2String(parsedMessage, 3);
            // Find the UUID of the avatar they want to freeze
            key userUUID = aaAvatarFullName2Key(userToFreezeFirst + " " + userToFreezeLast);
            // Now freeze the avatar
            aaFreezeAvatar(id);
        }
        else if(llList2String(parsedMessage, 0) == "thaw" &&
                llList2String(parsedMessage, 1) == "user")
        {
            // They want a user thawed by UUID
            string userToThawFirst = llList2String(parsedMessage, 2);
            string userToThawLast = llList2String(parsedMessage, 3);
            // Find the UUID of the avatar they want to thaw
            key userUUID = aaAvatarFullName2Key(userToThawFirst + " " + userToThawLast);
            // Now thaw the avatar
            aaThawAvatar(userUUID);
        }
        else if(llList2String(parsedMessage, 0) == "get" &&
                llList2String(parsedMessage, 1) == "walk" &&
                llList2String(parsedMessage, 2) == "disabled")
        {
            string userFirst = llList2String(parsedMessage, 3);
            string userLast = llList2String(parsedMessage, 4);
            // Find the UUID of the avatar they want to know about
            key userUUID = aaAvatarFullName2Key(userFirst + " " + userLast);
            // Now tell the info
            llSay(0, userFirst + " " + userLast + "'s walk ability is " + aaGetWalkDisabled(userUUID));
        }
        else if(llList2String(parsedMessage, 0) == "set" &&
                llList2String(parsedMessage, 1) == "walk" &&
                llList2String(parsedMessage, 2) == "disabled")
        {
            // This will block or unblock the given user from walking
            string userFirst = llList2String(parsedMessage, 3);
            string userLast = llList2String(parsedMessage, 4);
            integer frozen = llList2Integer(parsedMessage, 5);
            // Find the UUID of the avatar they want to know about
            key userUUID = aaAvatarFullName2Key(userFirst + " " + userLast);
            // Now set it
            aaSetWalkDisabled(userUUID, frozen);
        }
        else if(llList2String(parsedMessage, 0) == "get" &&
                llList2String(parsedMessage, 1) == "fly" &&
                llList2String(parsedMessage, 2) == "disabled")
        {
            string userFirst = llList2String(parsedMessage, 3);
            string userLast = llList2String(parsedMessage, 4);
            // Find the UUID of the avatar they want to know about
            key userUUID = aaAvatarFullName2Key(userFirst + " " + userLast);
            // Now tell the info
            llSay(0, userFirst + " " + userLast + "'s walk ability is " + aaGetFlyDisabled(userUUID));
        }
        else if(llList2String(parsedMessage, 0) == "set" &&
                llList2String(parsedMessage, 1) == "fly" &&
                llList2String(parsedMessage, 2) == "disabled")
        {
            // This will block or unblock the given user from flying
            string userFirst = llList2String(parsedMessage, 3);
            string userLast = llList2String(parsedMessage, 4);
            integer frozen = llList2Integer(parsedMessage, 5);
            // Find the UUID of the avatar they want to know about
            key userUUID = aaAvatarFullName2Key(userFirst + " " + userLast);
            // Now set it
            aaSetFlyDisabled(userUUID, frozen);
        }
    }
    run_time_permissions(integer perm)
    {
        // This is to check whether the avatar has accepted the combat 
        // permission that we requested from them in "join"
        // The special combat permission
        if (perm & PERMISSION_TAKE_CONTROLS | PERMISSION_COMBAT == PERMISSION_COMBAT)
        {
            // They accepted it, allow them into the combat now
            aaJoinCombat(savedID);
        }
    }
}