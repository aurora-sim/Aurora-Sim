default
{
    state_entry()
    {
        if(aaWindlightGetSceneIsStatic())
        {
            llSay(0, "The windlight scene must not be static to modify the day cycle");
        }
        else
        {
            //The max amount of day cycle keyframes there are
            integer dayCycleKeyFrames = aaWindlightGetSceneDayCycleKeyFrameCount();
            
            llSay(0,"There are " + dayCycleKeyFrames + " day cycle keyframes");
            
            list dayCycleFrames = aaWindlightGetDayCycle();
            integer i = 0;
            for(i = 0; i < llGetListLength(dayCycleFrames); i+=3)
            {
                integer presetNum = llList2Integer(dayCycleFrames, i);
                float dayCycleLocation = llList2Float(dayCycleFrames, i + 1);
                string presetName = llList2String(dayCycleFrames, i + 2);
                
                llSay(0,"Key frame " + presetNum + ": " + presetName + " at " + dayCycleLocation);
            }
            
            llSay(0, "Adding a new frame at 0.95 that looks the same as the first frame");
            
            aaWindlightAddDayCycleFrame(0.95, 0);
            
            llSay(0,"There are " + aaWindlightGetSceneDayCycleKeyFrameCount() + " day cycle keyframes now");
            
            dayCycleFrames = aaWindlightGetDayCycle();
            for(i = 0; i < llGetListLength(dayCycleFrames); i+=3)
            {
                integer presetNum = llList2Integer(dayCycleFrames, i);
                float dayCycleLocation = llList2Float(dayCycleFrames, i + 1);
                string presetName = llList2String(dayCycleFrames, i + 2);
                
                llSay(0,"Key frame " + presetNum + ": " + presetName + " at " + dayCycleLocation);
            }
            
            llSay(0, "Removing the last frame");
            
            aaWindlightRemoveDayCycleFrame(dayCycleKeyFrames);
            
            llSay(0,"There are " + aaWindlightGetSceneDayCycleKeyFrameCount() + " day cycle keyframes now");
            
            dayCycleFrames = aaWindlightGetDayCycle();
            for(i = 0; i < llGetListLength(dayCycleFrames); i+=3)
            {
                integer presetNum = llList2Integer(dayCycleFrames, i);
                float dayCycleLocation = llList2Float(dayCycleFrames, i + 1);
                string presetName = llList2String(dayCycleFrames, i + 2);
                
                llSay(0,"Key frame " + presetNum + ": " + presetName + " at " + dayCycleLocation);
            }
        }
    }
}