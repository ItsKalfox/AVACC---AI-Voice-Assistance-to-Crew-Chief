import os
import re
import csv

CONFIG_PATH = r"C:/Users/nipun/.gemini/antigravity/brain/120f7a01-7d04-459f-a642-43dbb5733abe/.user_uploaded/media_1793861715736_444b14bd.txt"
OUTPUT_CSV = r"c:/Projects/AVACC---AI-Voice-Assistance-to-Crew-Chief/CrewChief_All_Commands.csv"

# Read config
with open(CONFIG_PATH, "r", encoding="utf-8") as f:
    raw_lines = f.readlines()

config_map = {}
for line in raw_lines:
    line = line.strip()
    if not line or line.startswith("#") or line.startswith("//"):
        continue
    if "=" in line:
        k, v = line.split("=", 1)
        k = k.strip()
        v = v.strip()
        config_map[k] = v

print(f"Loaded {len(config_map)} key-value pairs from speech_recognition_config.txt")

# Helper to format clean voice line
def get_voice_line(cmd_id, default_val=None):
    if cmd_id in config_map:
        options = [opt.strip() for opt in config_map[cmd_id].split(":") if opt.strip()]
        if options:
            return options[0]
    return default_val or cmd_id.replace("_", " ").lower()

def get_all_phrases(cmd_id):
    if cmd_id in config_map:
        return [opt.strip() for opt in config_map[cmd_id].split(":") if opt.strip()]
    return []

# Categories & commands list
commands = []

def add_cmd(name, cmd_id, category, description, voice_line=None, examples=None, aliases=None, is_enabled=True):
    actual_voice = voice_line if voice_line else get_voice_line(cmd_id)
    raw_phrases = get_all_phrases(cmd_id)
    
    ex_list = []
    if examples:
        if isinstance(examples, list):
            ex_list.extend(examples)
        else:
            ex_list.extend([e.strip() for e in examples.split(";") if e.strip()])
    # add other phrases from config as examples if not already present
    for p in raw_phrases:
        if p != actual_voice and p not in ex_list:
            ex_list.append(p)
            
    al_list = []
    if aliases:
        if isinstance(aliases, list):
            al_list.extend(aliases)
        else:
            al_list.extend([a.strip() for a in aliases.split(";") if a.strip()])
            
    commands.append({
        "Name": name,
        "CrewChiefCommandId": cmd_id,
        "Category": category,
        "Description": description,
        "VoiceLine": actual_voice,
        "Examples": ";".join(ex_list[:6]),
        "Aliases": ";".join(al_list[:4]),
        "IsEnabled": "True" if is_enabled else "False"
    })

# 1. Tyres, Camber & Surface Temps
add_cmd("Tyre Wear Status", "HOWS_MY_TYRE_WEAR", "Tyres", "Report tyre wear levels across all four tyres", examples="how are my tyres;what's my tyre wear;tyre wear check", aliases="tyre wear;tires wear;wear")
add_cmd("Tyre Pressures", "WHAT_ARE_MY_TYRE_PRESSURES", "Tyres", "Report current tyre pressures in PSI", examples="what are my tire pressures;check tire pressures;give me pressures", aliases="pressures;psi;tire pressures")
add_cmd("Tyre Pressures Status", "HOW_ARE_MY_TYRE_PRESSURES", "Tyres", "Overall assessment of tyre pressure status", examples="how are my pressures;how are my tire pressures;pressures ok", aliases="pressures check;pressure status")
add_cmd("Front Tyre Pressures", "HOW_ARE_MY_FRONT_TYRE_PRESSURES", "Tyres", "Report front tyre pressures", examples="how are my front pressures;front tire pressures", aliases="front pressures;front tires")
add_cmd("Rear Tyre Pressures", "HOW_ARE_MY_REAR_TYRE_PRESSURES", "Tyres", "Report rear tyre pressures", examples="how are my rear pressures;rear tire pressures", aliases="rear pressures;rear tires")
add_cmd("Tyre Temperatures Status", "HOW_ARE_MY_TYRE_TEMPS", "Tyres", "Overall assessment of tyre temperatures", examples="how are my tire temps;are my tires warm;tyre temps ok", aliases="tyre temps;tire temps")
add_cmd("Tyre Temperatures", "WHAT_ARE_MY_TYRE_TEMPS", "Tyres", "Report numerical tyre temperatures for all four wheels", examples="what are my tire temperatures;check tire temps;read tire temps", aliases="temps;tire temp")
add_cmd("Current Tyre Compound", "WHAT_TYRES_AM_I_ON", "Tyres", "Report current tyre compound fitted on car", examples="what tires am I on;what rubber am I running;current compound", aliases="tire compound;compound;what tires")
add_cmd("Tyre Life Expectancy", "HOW_LONG_WILL_THESE_TYRES_LAST", "Tyres", "Estimate remaining laps/time on current set of tyres", examples="how long will the tires last;how long on these tires;will tyres make it to the end", aliases="tire life;tire longevity")
add_cmd("Tyre Compound Comparison", "WHAT_ARE_THE_RELATIVE_TYRE_PERFORMANCES", "Tyres", "Compare pace difference between tyre compounds", examples="give me tyre pace differences;compare tire compounds;what are the tire speeds", aliases="compound pace;tyre comparison")
add_cmd("Laps on Current Tyre Set", "HOW_MANY_LAPS_ON_TYRE_SET", "Tyres", "Report number of laps completed on the active tyre set", examples="how many laps on tire set;how many laps on these tires;tyre stint length", aliases="laps on set;stint laps")

# Camber commands
add_cmd("Front Camber Status", "HOWS_MY_FRONT_CAMBER", "Tyres", "Report front wheel camber alignment status", examples="how's my front camber;check front camber", aliases="front camber")
add_cmd("Rear Camber Status", "HOWS_MY_REAR_CAMBER", "Tyres", "Report rear wheel camber alignment status", examples="how's my rear camber;check rear camber", aliases="rear camber")
add_cmd("Left Front Camber", "HOWS_MY_LEFT_FRONT_CAMBER", "Tyres", "Report left front wheel camber", examples="how's my left front camber;lf camber", aliases="lf camber;left front camber")
add_cmd("Right Front Camber", "HOWS_MY_RIGHT_FRONT_CAMBER", "Tyres", "Report right front wheel camber", examples="how's my right front camber;rf camber", aliases="rf camber;right front camber")
add_cmd("Left Rear Camber", "HOWS_MY_LEFT_REAR_CAMBER", "Tyres", "Report left rear wheel camber", examples="how's my left rear camber;lr camber", aliases="lr camber;left rear camber")
add_cmd("Right Rear Camber", "HOWS_MY_RIGHT_REAR_CAMBER", "Tyres", "Report right rear wheel camber", examples="how's my right rear camber;rr camber", aliases="rr camber;right rear camber")

# Right Now variants
add_cmd("Front Camber Right Now", "HOWS_MY_FRONT_CAMBER_RIGHT_NOW", "Tyres", "Live dynamic front camber measurement right now", examples="how's my front camber right now;front camber live", is_enabled=False)
add_cmd("Rear Camber Right Now", "HOWS_MY_REAR_CAMBER_RIGHT_NOW", "Tyres", "Live dynamic rear camber measurement right now", examples="how's my rear camber right now;rear camber live", is_enabled=False)
add_cmd("Left Front Camber Right Now", "HOWS_MY_LEFT_FRONT_CAMBER_RIGHT_NOW", "Tyres", "Live left front camber right now", examples="how's my left front camber right now", is_enabled=False)
add_cmd("Right Front Camber Right Now", "HOWS_MY_RIGHT_FRONT_CAMBER_RIGHT_NOW", "Tyres", "Live right front camber right now", examples="how's my right front camber right now", is_enabled=False)
add_cmd("Left Rear Camber Right Now", "HOWS_MY_LEFT_REAR_CAMBER_RIGHT_NOW", "Tyres", "Live left rear camber right now", examples="how's my left rear camber right now", is_enabled=False)
add_cmd("Right Rear Camber Right Now", "HOWS_MY_RIGHT_REAR_CAMBER_RIGHT_NOW", "Tyres", "Live right rear camber right now", examples="how's my right rear camber right now", is_enabled=False)
add_cmd("Front Tyre Pressures Right Now", "HOW_ARE_MY_FRONT_TYRE_PRESSURES_RIGHT_NOW", "Tyres", "Live front tyre pressures right now", examples="how are my front pressures right now", is_enabled=False)
add_cmd("Rear Tyre Pressures Right Now", "HOW_ARE_MY_REAR_TYRE_PRESSURES_RIGHT_NOW", "Tyres", "Live rear tyre pressures right now", examples="how are my rear pressures right now", is_enabled=False)
add_cmd("Tyre Pressures Right Now", "HOW_ARE_MY_TYRE_PRESSURES_RIGHT_NOW", "Tyres", "Live tyre pressures right now across all 4 wheels", examples="how are my pressures right now", is_enabled=False)

# Surface temps
add_cmd("Left Front Surface Temps", "WHAT_ARE_MY_LEFT_FRONT_SURFACE_TEMPS", "Tyres", "Inner, middle, outer surface temperatures for left front", examples="what are my left front surface temps;lf surface temp", is_enabled=False)
add_cmd("Right Front Surface Temps", "WHAT_ARE_MY_RIGHT_FRONT_SURFACE_TEMPS", "Tyres", "Inner, middle, outer surface temperatures for right front", examples="what are my right front surface temps;rf surface temp", is_enabled=False)
add_cmd("Left Rear Surface Temps", "WHAT_ARE_MY_LEFT_REAR_SURFACE_TEMPS", "Tyres", "Inner, middle, outer surface temperatures for left rear", examples="what are my left rear surface temps;lr surface temp", is_enabled=False)
add_cmd("Right Rear Surface Temps", "WHAT_ARE_MY_RIGHT_REAR_SURFACE_TEMPS", "Tyres", "Inner, middle, outer surface temperatures for right rear", examples="what are my right rear surface temps;rr surface temp", is_enabled=False)

# 2. Brakes & Powertrain
add_cmd("Brakes Status", "HOWS_MY_BRAKES", "Brakes", "Report brake pad/disc condition and wear", examples="how are my brakes;how's my brakes;check brakes", aliases="brakes;brake condition")
add_cmd("Brake Temperatures Status", "HOW_ARE_MY_BRAKE_TEMPS", "Brakes", "Overall temperature status of brakes", examples="how are my brake temps;are my brakes cold;are brakes overheating", aliases="brake temps;brake temperature")
add_cmd("Brake Temperatures", "WHAT_ARE_MY_BRAKE_TEMPS", "Brakes", "Report numerical brake disc temperatures", examples="what are my brake temps;read brake temperatures", aliases="brake temperature check;disc temps")
add_cmd("Engine Status", "HOWS_MY_ENGINE", "Powertrain", "Report engine health and damage", examples="how's my engine;is the motor ok;engine health", aliases="engine;motor;engine health")
add_cmd("Engine Temperatures Status", "HOW_ARE_MY_ENGINE_TEMPS", "Powertrain", "Overall temperature assessment of engine", examples="how are my engine temps;is the engine running hot", aliases="engine temps")
add_cmd("Engine Temperatures", "WHAT_ARE_MY_ENGINE_TEMPS", "Powertrain", "Report water and oil temperatures", examples="what are my engine temps;engine temperatures", aliases="engine readouts;powertrain temps")
add_cmd("Water Temperature", "WHAT_IS_MY_WATER_TEMP", "Powertrain", "Report water/coolant temperature", examples="whats my water temp;water temperature check", aliases="water temp;coolant temp")
add_cmd("Oil Temperature", "WHAT_IS_MY_OIL_TEMP", "Powertrain", "Report engine oil temperature", examples="whats my oil temp;oil temperature check", aliases="oil temp")
add_cmd("Transmission Status", "HOWS_MY_TRANSMISSION", "Powertrain", "Report gearbox and transmission health", examples="how's my transmission;how's my gearbox;is gearbox ok", aliases="transmission;gearbox")
add_cmd("Battery / Hybrid Status", "HOWS_MY_BATTERY", "Powertrain", "Report battery state of charge or hybrid system energy level", examples="how's my battery;what's my battery level;energy level", aliases="battery;hybrid;ers;soc")

# 3. Damage & Chassis
add_cmd("Damage Report", "DAMAGE_REPORT", "Damage", "Full vehicle damage report including aero, suspension and engine", examples="damage report;how's my car;is my car ok;check damage", aliases="damage;car condition;wreck check")
add_cmd("Aero / Bodywork Status", "HOWS_MY_AERO", "Damage", "Report front and rear aerodynamic and bodywork damage", examples="how's my aero;how's my bodywork;is my wing damaged", aliases="aero;bodywork;downforce")
add_cmd("Suspension Status", "HOWS_MY_SUSPENSION", "Damage", "Report suspension damage and alignment condition", examples="how's my suspension;any suspension damage", aliases="suspension")
add_cmd("Car Status Overview", "CAR_STATUS", "Damage", "Quick overview of vehicle condition", examples="car status;vehicle status;how is the car", aliases="car status;car condition")
add_cmd("Session Status Overview", "SESSION_STATUS", "Session", "High-level overview of current session progress and position", examples="session status;race status;how is the race going", aliases="session status;race status")
add_cmd("Full Race Status Update", "STATUS", "Session", "Complete comprehensive briefing from crew chief", examples="full update;full status;update me;give me full briefing", aliases="status;full briefing;sitrep")

# 4. Fuel & Calculation
add_cmd("Current Fuel Level", "WHATS_MY_FUEL_LEVEL", "Fuel", "Report litres or gallons of fuel remaining in tank", examples="what's my fuel level;how much fuel is left;fuel remaining", aliases="fuel level;fuel remaining;litres left")
add_cmd("Fuel Status Overview", "HOWS_MY_FUEL", "Fuel", "Overall fuel status and burn rate", examples="how's my fuel;how is our fuel looking", aliases="fuel check;fuel status")
add_cmd("Fuel Consumption Rate", "WHATS_MY_FUEL_USAGE", "Fuel", "Report average fuel consumption per lap", examples="what's my fuel usage;what's my fuel consumption;fuel use per lap", aliases="fuel usage;consumption;fuel per lap")
add_cmd("Fuel Needed to Finish Race", "HOW_MUCH_FUEL_TO_END_OF_RACE", "Fuel", "Calculate fuel required to reach chequered flag", examples="how much fuel to the end;how much fuel do we need;will fuel last to the end", aliases="fuel to end;finish fuel")
add_cmd("Calculate Fuel Requirement", "CALCULATE_FUEL_FOR", "Fuel", "Estimate fuel load needed for specific laps or minutes (e.g. 'calculate fuel for 25 laps')", voice_line="calculate fuel for {amount} {unit}", examples="calculate fuel for 20 laps;how much fuel do I need for 45 minutes;fuel calculation for 30 laps", aliases="calc fuel;fuel for laps;fuel for time")

# 5. Timing & Gaps
add_cmd("Gap to Car Ahead", "WHATS_MY_GAP_IN_FRONT", "Gaps", "Report time delta to the car ahead", examples="what's the gap ahead;what's my gap in front;gap ahead;how far to the guy in front", aliases="gap ahead;gap in front;delta ahead")
add_cmd("Gap to Car Behind", "WHATS_MY_GAP_BEHIND", "Gaps", "Report time delta to the car chasing behind", examples="what's the gap behind;what's my gap behind;gap behind;how close is the guy behind", aliases="gap behind;delta behind")
add_cmd("My Last Lap Time", "WHAT_WAS_MY_LAST_LAP_TIME", "Timing", "Announce lap time achieved on the previous lap", examples="what's my last lap time;last lap time;lap time;what was my last lap", aliases="last lap;lap time;previous lap")
add_cmd("My Best Lap Time", "WHATS_MY_BEST_LAP_TIME", "Timing", "Announce personal fastest lap time of session", examples="what's my best lap time;best lap time;what is my fastest lap", aliases="best lap;fastest lap;pb lap")
add_cmd("Overall Fastest Lap Time", "WHATS_THE_FASTEST_LAP_TIME", "Timing", "Announce the overall session fastest lap time", examples="what's the fastest lap;what's the fastest lap time;who has purple lap", aliases="session best;purple lap;fastest lap time")
add_cmd("My Sector Times", "WHAT_ARE_MY_SECTOR_TIMES", "Timing", "Report sector times for the last lap", examples="what are my sector times;sector times;read sectors", aliases="sectors;sector times;split times")
add_cmd("My Last Sector Time", "WHATS_MY_LAST_SECTOR_TIME", "Timing", "Report time for the most recently crossed sector", examples="what's my last sector time;last sector;how was sector two", aliases="last sector;split time")
add_cmd("My Overall Pace", "HOWS_MY_PACE", "Timing", "Crew Chief evaluates your general lap pace compared to competitors", examples="how's my pace;am I fast enough;how is my lap pace", aliases="pace;race pace")
add_cmd("My Personal Delta / Pace", "HOWS_MY_SELF_PACE", "Timing", "Evaluate personal delta against personal best", examples="how's my personal pace;how's my self gap;how's my delta best", aliases="delta pace;personal delta")
add_cmd("Where Am I Faster", "WHERE_AM_I_FASTER", "Gaps", "Report sectors or corners where you gain time on competitor", examples="where am I faster;where should I attack;where can I overtake", aliases="attack;where faster")
add_cmd("Where Am I Slower", "WHERE_AM_I_SLOWER", "Gaps", "Report sectors or corners where competitor is catching you", examples="where am I slower;where should I defend;where is he faster", aliases="defend;where slower")

# 6. Session & Standings
add_cmd("My Current Race Position", "WHATS_MY_POSITION", "Session", "Announce current position in class and race", examples="what's my position;what position am I in;what P am I", aliases="position;race position;my place")
add_cmd("Expected Finish Position", "WHATS_MY_EXPECTED_FINISH_POSITION", "Session", "Predicted finish position based on current pace and pitstops", examples="what's my expected finish position;what's my expected position", aliases="expected position;finish prediction")
add_cmd("Time / Laps Remaining in Session", "HOW_LONGS_LEFT", "Session", "Report time or laps remaining before race end", examples="how long's left;how many laps left;how many laps to go;time remaining", aliases="time left;laps left;duration")
add_cmd("Current Time of Day", "WHATS_THE_TIME", "Session", "Report in-game or real-world clock time", examples="what's the time;what time is it", aliases="time;clock")
add_cmd("Who is Leading the Race", "WHOS_LEADING", "Opponents", "Announce who is in first place / leading the race", examples="who's leading;who's in the lead;who's the leader;who is in P1", aliases="race leader;p1;who leads")
add_cmd("Who is Ahead in Race Position", "WHOS_IN_FRONT_IN_THE_RACE", "Opponents", "Identify competitor directly ahead in race standings", examples="who's in front in the race;who's ahead in the race;who's in front", aliases="car ahead;who is ahead")
add_cmd("Who is Behind in Race Position", "WHOS_BEHIND_IN_THE_RACE", "Opponents", "Identify competitor directly behind in race standings", examples="who's behind in the race;who's behind", aliases="car behind;who is behind")
add_cmd("Who is Physically Ahead on Track", "WHOS_IN_FRONT_ON_TRACK", "Opponents", "Identify the physical car in front on tarmac (could be backmarker)", examples="who's in front on track;who is ahead on track", aliases="track ahead;car in front track")
add_cmd("Who is Physically Behind on Track", "WHOS_BEHIND_ON_TRACK", "Opponents", "Identify physical car behind on tarmac", examples="who's behind on track;car behind on track", aliases="track behind;car behind track")
add_cmd("Is Car Ahead Same Class", "IS_CAR_AHEAD_MY_CLASS", "Opponents", "Verify whether competitor ahead is fighting for your class position", examples="is the car ahead in my class;is the car ahead my class;same class ahead", aliases="class check ahead;ahead class")
add_cmd("Is Car Behind Same Class", "IS_CAR_BEHIND_MY_CLASS", "Opponents", "Verify whether car behind is in your class", examples="is the car behind in my class;is car behind my class", aliases="class check behind;behind class")
add_cmd("What Class is Car Ahead", "WHAT_CLASS_IS_CAR_AHEAD", "Opponents", "Report car class of vehicle in front", examples="what class is the car ahead;what class is the guy in front", aliases="car class ahead")
add_cmd("What Class is Car Behind", "WHAT_CLASS_IS_CAR_BEHIND", "Opponents", "Report car class of vehicle behind", examples="what class is the car behind;what class is the guy behind", aliases="car class behind")
add_cmd("Air Temperature", "WHATS_THE_AIR_TEMP", "Session", "Report ambient air temperature", examples="what's the air temp;air temperature check", aliases="air temp;ambient temp")
add_cmd("Track Temperature", "WHATS_THE_TRACK_TEMP", "Session", "Report track surface temperature", examples="what's the track temp;track temperature check", aliases="track temp;asphalt temp")
add_cmd("My iRating", "WHATS_MY_IRATING", "Session", "Report personal iRating (iRacing only)", examples="whats my eye rating;what's my irating", aliases="irating;rating")
add_cmd("My License Class", "WHATS_MY_LICENSE_CLASS", "Session", "Report personal license class (iRacing only)", examples="what's my licence class;check license class", aliases="license class;safety rating")
add_cmd("My Driver Rank", "WHATS_MY_RANK", "Session", "Report driver rank score", examples="what's my rank;driver ranking", aliases="rank;ranking")
add_cmd("My Rating", "WHATS_MY_RATING", "Session", "Report driver overall skill rating", examples="what's my rating", aliases="driver rating")
add_cmd("My Reputation Score", "WHATS_MY_REPUTATION", "Session", "Report driver reputation / safety metric", examples="what's my reputation", aliases="reputation;safety")
add_cmd("Incident Points Count", "HOW_MANY_INCIDENT_POINTS", "Session", "Report accumulated incident points in current session", examples="how many incidents do i have;what's my incident count;incident points", aliases="incidents;incident points;x count")
add_cmd("Incident Points Limit", "WHATS_THE_INCIDENT_LIMIT", "Session", "Report session maximum incident limit before penalty/disqualification", examples="what's the incident limit;incident points limit", aliases="incident limit;max incidents")
add_cmd("Strength of Field (SOF)", "WHATS_THE_SOF", "Session", "Report Strength of Field calculation for race", examples="what's the sof;what is the strength of field", aliases="sof;strength of field")
add_cmd("Stop Monitoring All Drivers", "STOP_WATCHING_ALL", "Opponents", "Cancel all actively monitored opponents", examples="stop monitoring drivers;cancel monitored drivers;stop monitoring all", aliases="unmonitor;stop watching")

# 7. Radio Comms & Spotter
add_cmd("Radio Check", "RADIO_CHECK", "Comms", "Test audio bridge and microphone check with Crew Chief", voice_line="can you hear me", examples="can you hear me;radio check;mic check;radio test", aliases="radio check;mic check;audio test")
add_cmd("Repeat Last Message", "REPEAT_LAST_MESSAGE", "Comms", "Ask crew chief to say again the previous radio message", examples="repeat last message;say again;come again", aliases="repeat;say again")
add_cmd("Request More Information", "MORE_INFO", "Comms", "Ask crew chief to expand or clarify on previous message", examples="clarify;tell me more;more information;more info", aliases="clarify;more info")
add_cmd("Confirm Driver is OK", "I_AM_OK", "Comms", "Respond to crew chief inquiry after a crash/incident", examples="yeah;fine;I'm OK;I'm fine;yes I'm fine", aliases="im ok;fine;driver ok")
add_cmd("Keep Quiet / Minimal Comms", "KEEP_QUIET", "Comms", "Silence crew chief non-essential chatter", examples="keep quiet;shut up;leave me alone;minimal talking", aliases="keep quiet;silence;shut up;quiet")
add_cmd("Keep Me Informed / Regular Updates", "KEEP_ME_INFORMED", "Comms", "Request regular updates from crew chief", examples="keep me updated;keep me informed;keep me posted", aliases="keep informed;updates")
add_cmd("Announce Lap Deltas and Gaps", "TELL_ME_THE_GAPS", "Comms", "Instruct crew chief to call time deltas every lap", examples="tell me the gaps;tell me the deltas;give me the deltas", aliases="tell gaps;deltas on")
add_cmd("Stop Announcing Deltas and Gaps", "DONT_TELL_ME_THE_GAPS", "Comms", "Disable automatic lap delta callouts", examples="don't tell me the gaps;don't tell me the deltas;no more deltas;no more gaps", aliases="stop gaps;deltas off")
add_cmd("No Messages in Braking Zones", "DONT_TALK_IN_THE_CORNERS", "Comms", "Suppress radio chatter during cornering and braking", examples="don't talk in the corners;no messages in the corners;no messages in braking zones", aliases="quiet in corners;corner silence")
add_cmd("Allow Messages Anywhere on Track", "TALK_TO_ME_ANYWHERE", "Comms", "Allow crew chief to talk anywhere around the lap", examples="talk to me anywhere;messages at any point", aliases="talk anywhere")
add_cmd("Enable Spotter Callouts", "SPOT", "Comms", "Enable spotter car left / car right spotter calls", examples="spot;start spotting;enable spotter", aliases="enable spotter;start spotting;spotter on")
add_cmd("Disable Spotter Callouts", "DONT_SPOT", "Comms", "Mute spotter audio callouts", examples="don't spot;disable spotter;stop spotting", aliases="disable spotter;don't spot;spotter off")
add_cmd("Stop Complaining / Grumbling", "STOP_COMPLAINING", "Comms", "Tell crew chief to stop grumbling about bad lap times or mistakes", examples="stop complaining;stop grumbling", aliases="stop whining;stop grumbling")
add_cmd("Announce Corner Names", "PLAY_CORNER_NAMES", "Comms", "Crew chief reads names of upcoming corners", examples="read corner names;corner names;tell me corner names", aliases="corner names")

# 8. Rules & Penalties
add_cmd("Check for Active Penalty", "DO_I_HAVE_A_PENALTY", "Session", "Ask if you have received a drive-through or time penalty", examples="do I have a penalty;penalty check;any penalties", aliases="penalty check;active penalty")
add_cmd("Check if Penalty Still Applies", "DO_I_STILL_HAVE_A_PENALTY", "Session", "Ask if a previously issued penalty is still pending", examples="do I still have a penalty;is penalty still active", aliases="still penalty;pending penalty")
add_cmd("Check if Penalty Was Served", "HAVE_I_SERVED_MY_PENALTY", "Session", "Confirm whether penalty was served on previous pit entry", examples="have I served my penalty;is penalty served", aliases="served penalty")
add_cmd("Enable Cut Track Warnings", "ENABLE_CUT_TRACK_WARNINGS", "Session", "Turn on track boundary / cut track warnings", examples="enable cut track warnings;play cut track warnings;warn about cuts", aliases="enable cut warnings;cut track on")
add_cmd("Disable Cut Track Warnings", "DISABLE_CUT_TRACK_WARNINGS", "Session", "Turn off cut track warnings", examples="no cut track warnings;no more cut warnings;stop cut warnings", aliases="disable cut warnings;cut track off")
add_cmd("Enable Yellow Flag Updates", "ENABLE_YELLOW_FLAG_MESSAGES", "Session", "Enable incident and local yellow flag callouts", examples="give me yellows;tell me yellows;give me incident updates", aliases="yellows on;yellow flags on")
add_cmd("Disable Yellow Flag Updates", "DISABLE_YELLOW_FLAG_MESSAGES", "Session", "Mute yellow flag updates", examples="no more yellows;stop incident updates;don't give me yellows", aliases="yellows off;yellow flags off")
add_cmd("Rolling Start Formation Lap", "ENABLE_MANUAL_FORMATION_LAP", "Session", "Signal to crew chief that current lap is a rolling start formation lap", examples="this is the formation lap;formation lap;rolling start", aliases="formation lap;rolling start")
add_cmd("Standing Start No Formation", "DISABLE_MANUAL_FORMATION_LAP", "Session", "Signal standing start with no formation lap", examples="standing start;no formation lap", aliases="standing start;no formation")

# 9. Pitstop Management
add_cmd("Check Mandatory Pitstop Requirement", "DO_I_HAVE_A_MANDATORY_PIT_STOP", "Pitstop", "Check if race regulations mandate a pitstop", examples="do I have to pit;do I need to pit;do I have a mandatory pit stop;mandatory stop", aliases="mandatory pit;mandatory stop;pit required")
add_cmd("Check if Pit Stall is Occupied", "IS_MY_PIT_BOX_OCCUPIED", "Pitstop", "Check whether team mate or competitor is currently in your pit stall", examples="is my pit box occupied;is my pit stall occupied;is pit free", aliases="pit occupied;pit box clear")
add_cmd("Pitlane Speed Limit", "WHATS_PITLANE_SPEED_LIMIT", "Pitstop", "Report maximum speed limit enforced in pitlane", examples="what's the pitlane speed limit;what's the pit speed limit;pit speed limit", aliases="pit speed limit;pitlane limit")
add_cmd("Estimated Position After Pitstop", "PLAY_POST_PIT_POSITION_ESTIMATE", "Pitstop", "Predict track position upon exiting pitlane", examples="where will i be after a stop;estimate pit exit positions;what happens if i pit", aliases="post pit position;pit exit position")
add_cmd("Time / Benchmark Pitstop", "PRACTICE_PIT_STOP", "Pitstop", "Time the duration of practice stop in box", examples="time this stop;practice pitstop;pitstop benchmark", aliases="practice pit;benchmark pit")
add_cmd("Check Current Pitstop Plan", "WHAT_ARE_THE_PIT_ACTIONS", "Pitstop", "List all services scheduled for next pitstop (fuel, tyres, repairs)", examples="what are the pit actions;what's the pitstop plan;check pit actions", aliases="pit plan;pit actions")
add_cmd("Pitstop Add Fuel / Service", "PIT_STOP_ADD", "Pitstop", "Queue fuel addition in pitstop menu", voice_line="pitstop add", examples="pitstop add;box, add;add fuel", aliases="pit add;box add")
add_cmd("Pitstop Fuel to End of Race", "PIT_STOP_FUEL_TO_THE_END", "Pitstop", "Automatically calculate and add fuel needed to finish the race", voice_line="pitstop fuel to the end", examples="pitstop fuel to the end;box, fuel to the end;fuel to finish", aliases="pit fuel to end;box fuel end")
add_cmd("Pitstop Enable Refueling", "PIT_STOP_REFUEL", "Pitstop", "Set pit menu to refuel car", voice_line="pitstop refuel", examples="pitstop refuel;box, refuel;refuel car", aliases="pit refuel;box refuel")
add_cmd("Pitstop Disable Refueling", "PIT_STOP_DONT_REFUEL", "Pitstop", "Clear fuel from pit menu (no refueling)", voice_line="pitstop don't refuel", examples="pitstop don't refuel;box, don't refuel;clear fuel", aliases="pit no fuel;don't refuel")
add_cmd("Pitstop Clear Fuel", "PIT_STOP_CLEAR_FUEL", "Pitstop", "Reset fuel quantity to zero in pit menu", voice_line="pitstop clear fuel", examples="pitstop clear fuel;box, clear fuel", aliases="clear fuel;no fuel")
add_cmd("Pitstop Clear All Work", "PIT_STOP_CLEAR_ALL", "Pitstop", "Deselect all tyre changes, repairs, and refueling", voice_line="pitstop clear all", examples="pitstop clear all;box, clear all;cancel pit work", aliases="pit clear all;box clear all")
add_cmd("Pitstop Windscreen Tearoff", "PIT_STOP_TEAROFF", "Pitstop", "Request windscreen tearoff strip removal", voice_line="pitstop tearoff", examples="pitstop tearoff;box, tearoff;clean windscreen", aliases="tearoff;windscreen")
add_cmd("Pitstop Clear Tearoff", "PIT_STOP_CLEAR_WIND_SCREEN", "Pitstop", "Deselect windscreen tearoff", voice_line="pitstop clear tearoff", examples="pitstop clear tearoff;box, clear windscreen", aliases="clear tearoff")
add_cmd("Pitstop Fast Repair", "PIT_STOP_FAST_REPAIR", "Pitstop", "Use quick reset repair in iRacing", voice_line="pitstop fast repair", examples="pitstop fast repair;box, fast repair;pitstop repair", aliases="fast repair;quick repair")
add_cmd("Pitstop Clear Fast Repair", "PIT_STOP_CLEAR_FAST_REPAIR", "Pitstop", "Save fast repair for later", voice_line="pitstop clear fast repair", examples="pitstop clear fast repair;box, clear fast repair", aliases="clear fast repair")
add_cmd("Pitstop Change All Tyres", "PIT_STOP_CHANGE_ALL_TYRES", "Pitstop", "Select all 4 tyres for change during pitstop", voice_line="pitstop change all tyres", examples="pitstop change all tyres;box, change all tyres;four new tires", aliases="change all tires;new tyres")
add_cmd("Pitstop Change Tyres", "PIT_STOP_CHANGE_TYRES", "Pitstop", "Toggle tyre change on in pit menu", voice_line="pitstop change tyres", examples="pitstop change tyres;box, change tyres", aliases="change tyres;box change tyres")
add_cmd("Pitstop Don't Change Tyres", "PIT_STOP_CLEAR_TYRES", "Pitstop", "Cancel tyre change (fuel only stop)", voice_line="pitstop clear tyres", examples="pitstop clear tyres;box, clear tyres;pitstop don't change tyres", aliases="clear tyres;no tyre change")
add_cmd("Pitstop Change Left Front Tyre", "PIT_STOP_CHANGE_FRONT_LEFT_TYRE", "Pitstop", "Select front left tyre for replacement", voice_line="pitstop change left front tyre", examples="pitstop change left front tyre;box, change left front tyre", aliases="change lf;change left front")
add_cmd("Pitstop Change Right Front Tyre", "PIT_STOP_CHANGE_FRONT_RIGHT_TYRE", "Pitstop", "Select front right tyre for replacement", voice_line="pitstop change right front tyre", examples="pitstop change right front tyre;box, change right front tyre", aliases="change rf;change right front")
add_cmd("Pitstop Change Left Rear Tyre", "PIT_STOP_CHANGE_REAR_LEFT_TYRE", "Pitstop", "Select rear left tyre for replacement", voice_line="pitstop change left rear tyre", examples="pitstop change left rear tyre;box, change left rear tyre", aliases="change lr;change left rear")
add_cmd("Pitstop Change Right Rear Tyre", "PIT_STOP_CHANGE_REAR_RIGHT_TYRE", "Pitstop", "Select rear right tyre for replacement", voice_line="pitstop change right rear tyre", examples="pitstop change right rear tyre;box, change right rear tyre", aliases="change rr;change right rear")
add_cmd("Pitstop Change Left Side Tyres", "PIT_STOP_CHANGE_LEFT_SIDE_TYRES", "Pitstop", "Select both left side tyres for change", voice_line="pitstop change left side tyres", examples="pitstop change left side tyres;box, change left side tyres", aliases="change left side")
add_cmd("Pitstop Change Right Side Tyres", "PIT_STOP_CHANGE_RIGHT_SIDE_TYRES", "Pitstop", "Select both right side tyres for change", voice_line="pitstop change right side tyres", examples="pitstop change right side tyres;box, change right side tyres", aliases="change right side")
add_cmd("Pitstop Change Front Tyres Only", "PIT_STOP_CHANGE_FRONT_TYRES", "Pitstop", "Select front tyres only for replacement", voice_line="pitstop change front tyres only", examples="pitstop change front tyres only;box, change front tyres only", aliases="change front tyres")
add_cmd("Pitstop Change Rear Tyres Only", "PIT_STOP_CHANGE_REAR_TYRES", "Pitstop", "Select rear tyres only for replacement", voice_line="pitstop change rear tyres only", examples="pitstop change rear tyres only;box, change rear tyres only", aliases="change rear tyres")
add_cmd("Pitstop Change Tyre Pressures", "PIT_STOP_CHANGE_TYRE_PRESSURE", "Pitstop", "Adjust scheduled pit tyre pressures", voice_line="pitstop change tyre pressures", examples="pitstop change tyre pressures;box, change tyre pressures", aliases="adjust pit pressures")
add_cmd("Pitstop Change Front Pressures", "PIT_STOP_CHANGE_FRONT_PRESSURES", "Pitstop", "Adjust front pit tyre pressures", voice_line="pitstop change front pressures", examples="pitstop change front pressures;box change front pressures", aliases="front pit pressures")
add_cmd("Pitstop Change Rear Pressures", "PIT_STOP_CHANGE_REAR_PRESSURES", "Pitstop", "Adjust rear pit tyre pressures", voice_line="pitstop change rear pressures", examples="pitstop change rear pressures;box change rear pressures", aliases="rear pit pressures")
add_cmd("Pitstop Change Left Front Pressure", "PIT_STOP_CHANGE_FRONT_LEFT_TYRE_PRESSURE", "Pitstop", "Set LF pressure for next set", voice_line="pitstop change left front tyre pressure", examples="pitstop change left front tyre pressure", aliases="lf pit pressure")
add_cmd("Pitstop Change Right Front Pressure", "PIT_STOP_CHANGE_FRONT_RIGHT_TYRE_PRESSURE", "Pitstop", "Set RF pressure for next set", voice_line="pitstop change right front tyre pressure", examples="pitstop change right front tyre pressure", aliases="rf pit pressure")
add_cmd("Pitstop Change Left Rear Pressure", "PIT_STOP_CHANGE_REAR_LEFT_TYRE_PRESSURE", "Pitstop", "Set LR pressure for next set", voice_line="pitstop change left rear tyre pressure", examples="pitstop change left rear tyre pressure", aliases="lr pit pressure")
add_cmd("Pitstop Change Right Rear Pressure", "PIT_STOP_CHANGE_REAR_RIGHT_TYRE_PRESSURE", "Pitstop", "Set RR pressure for next set", voice_line="pitstop change right rear tyre pressure", examples="pitstop change right rear tyre pressure", aliases="rr pit pressure")
add_cmd("Pitstop Select Tyre Set", "PIT_STOP_SELECT_TYRE_SET", "Pitstop", "Select designated tyre set number in ACC", voice_line="pitstop tire set", examples="pitstop tire set;box tire set", aliases="select tyre set;tire set")
add_cmd("Pitstop Select Best Tyre Set", "PIT_STOP_SELECT_LEAST_USED_TYRE_SET", "Pitstop", "Automatically select least used / best tyre set in ACC", voice_line="pitstop select best tires", examples="pitstop select best tires;box select best tyres", aliases="select best tyres;least used tires")

# Compounds
add_cmd("Pitstop Next Compound", "PIT_STOP_NEXT_TYRE_COMPOUND", "Pitstop", "Cycle to next available tyre compound in pit menu", voice_line="pitstop next tire compound", examples="pitstop next tire compound;box, next tire compound", aliases="next compound")
add_cmd("Pitstop Hard Tyres", "PIT_STOP_HARD_TYRES", "Pitstop", "Select Hard compound tyres for pitstop", voice_line="pitstop hard tires", examples="pitstop hard tires;box, hard tires", aliases="hard compound;put on hards")
add_cmd("Pitstop Medium Tyres", "PIT_STOP_MEDIUM_TYRES", "Pitstop", "Select Medium compound tyres for pitstop", voice_line="pitstop medium tires", examples="pitstop medium tires;box, medium tires", aliases="medium compound;put on mediums")
add_cmd("Pitstop Soft Tyres", "PIT_STOP_SOFT_TYRES", "Pitstop", "Select Soft compound tyres for pitstop", voice_line="pitstop soft tires", examples="pitstop soft tires;box, soft tires", aliases="soft compound;put on softs")
add_cmd("Pitstop Super Soft Tyres", "PIT_STOP_SUPERSOFT_TYRES", "Pitstop", "Select Super Soft compound tyres", voice_line="pitstop super soft tires", examples="pitstop super soft tires", aliases="supersofts")
add_cmd("Pitstop Ultra Soft Tyres", "PIT_STOP_ULTRASOFT_TYRES", "Pitstop", "Select Ultra Soft compound tyres", voice_line="pitstop ultra soft tires", examples="pitstop ultra soft tires", aliases="ultrasofts")
add_cmd("Pitstop Hyper Soft Tyres", "PIT_STOP_HYPERSOFT_TYRES", "Pitstop", "Select Hyper Soft compound tyres", voice_line="pitstop hyper soft tires", examples="pitstop hyper soft tires", aliases="hypersofts")
add_cmd("Pitstop Intermediate Tyres", "PIT_STOP_INTERMEDIATE_TYRES", "Pitstop", "Select Intermediate damp weather tyres", voice_line="pitstop intermediate tyres", examples="pitstop intermediate tyres;box, intermediate tyres", aliases="intermediates;inters")
add_cmd("Pitstop Wet Tyres", "PIT_STOP_WET_TYRES", "Pitstop", "Select Wet rain tyres for pitstop", voice_line="pitstop wet tyres", examples="pitstop wet tyres;box, wet tyres", aliases="wets;rain tyres")
add_cmd("Pitstop Dry Tyres", "PIT_STOP_DRY_TYRES", "Pitstop", "Select Slick dry weather tyres", voice_line="pitstop dry tyres", examples="pitstop dry tyres;box, dry tyres", aliases="dries;slicks")
add_cmd("Pitstop Monsoon Tyres", "PIT_STOP_MONSOON_TYRES", "Pitstop", "Select heavy rain Monsoon tyres", voice_line="pitstop monsoon tyres", examples="pitstop monsoon tyres;box, monsoon tyres", aliases="monsoon tyres;heavy rain tyres")
add_cmd("Pitstop Prime Tyres", "PIT_STOP_PRIME_TYRES", "Pitstop", "Select Prime compound tyres", voice_line="pitstop prime tires", examples="pitstop prime tires;pitstop primary tires", aliases="prime tires")
add_cmd("Pitstop Option Tyres", "PIT_STOP_OPTION_TYRES", "Pitstop", "Select Option compound tyres", voice_line="pitstop option tires", examples="pitstop option tires", aliases="option tires")
add_cmd("Pitstop Alternate Tyres", "PIT_STOP_ALTERNATE_TYRES", "Pitstop", "Select Alternate compound tyres", voice_line="pitstop alternate tyres", examples="pitstop alternate tyres", aliases="alternate tires")

# Pit repairs & penalty serving
add_cmd("Pitstop Fix Front Aero", "PIT_STOP_FIX_FRONT_AERO", "Pitstop", "Repair front splitter / aero damage in pitstop", voice_line="pitstop fix front aero only", examples="pitstop fix front aero only;fix front wing", aliases="repair front aero;fix front splitter")
add_cmd("Pitstop Fix Rear Aero", "PIT_STOP_FIX_REAR_AERO", "Pitstop", "Repair rear wing / aero damage in pitstop", voice_line="pitstop fix rear aero only", examples="pitstop fix rear aero only;fix rear wing", aliases="repair rear aero")
add_cmd("Pitstop Fix All Aero", "PIT_STOP_FIX_ALL_AERO", "Pitstop", "Repair all aerodynamic damage during pitstop", voice_line="pitstop fix all aero", examples="pitstop fix all aero;box, fix all aero", aliases="repair all aero;fix bodywork")
add_cmd("Pitstop Don't Fix Aero", "PIT_STOP_FIX_NO_AERO", "Pitstop", "Skip aero repairs to save pit time", voice_line="pitstop don't fix aero", examples="pitstop don't fix aero;box, don't fix aero;skip aero repair", aliases="no aero repair;skip aero")
add_cmd("Pitstop Fix Suspension", "PIT_STOP_FIX_SUSPENSION", "Pitstop", "Repair suspension and steering damage", voice_line="pitstop fix suspension", examples="pitstop fix suspension;box, fix suspension", aliases="repair suspension;fix steering")
add_cmd("Pitstop Don't Fix Suspension", "PIT_STOP_DONT_FIX_SUSPENSION", "Pitstop", "Skip suspension repair to save time", voice_line="pitstop don't fix suspension", examples="pitstop don't fix suspension;skip suspension", aliases="skip suspension")
add_cmd("Pitstop Serve Penalty", "PIT_STOP_SERVE_PENALTY", "Pitstop", "Serve stop-and-go or hold penalty on this stop", voice_line="pitstop serve penalty", examples="pitstop serve penalty;box, serve penalty", aliases="serve penalty;take penalty")
add_cmd("Pitstop Don't Serve Penalty", "PIT_STOP_DONT_SERVE_PENALTY", "Pitstop", "Postpone serving penalty to a later lap", voice_line="pitstop don't serve penalty", examples="pitstop don't serve penalty;skip penalty", aliases="skip penalty;postpone penalty")
add_cmd("Pitstop Fix All Damage", "PIT_STOP_FIX_ALL", "Pitstop", "Fix all mechanical and body damage in pitstop", voice_line="pitstop fix all", examples="pitstop fix all;box, fix all;pitstop repair all", aliases="fix all;repair all damage")
add_cmd("Pitstop Fix Body Only", "PIT_STOP_FIX_BODY", "Pitstop", "Repair bodywork panels only", voice_line="pitstop fix body", examples="pitstop fix body;pitstop repair body", aliases="fix body;repair body")
add_cmd("Pitstop Fix Nothing", "PIT_STOP_FIX_NONE", "Pitstop", "Do not perform any repairs during pitstop", voice_line="pitstop fix none", examples="pitstop fix none;pitstop repair none", aliases="no repairs;fix nothing")
add_cmd("Pitstop Fill To Target", "PIT_STOP_FILL_TO", "Pitstop", "Fill fuel tank to designated target quantity", voice_line="pitstop fill to", examples="pitstop fill to;box, fill to", aliases="fill to;target fuel")

# 10. MFD & In-Game Displays
add_cmd("Display Sector Times on MFD", "DISPLAY_SECTORS", "Display", "Switch MFD display to sector times page", examples="display sectors;display sector times;M F D sectors", aliases="show sectors;mfd sectors")
add_cmd("Display Pit Menu on MFD", "DISPLAY_PIT_MENU", "Display", "Switch MFD display to pitstop menu page", examples="display pit menu;M F D pit menu;open pit menu", aliases="show pit menu;mfd pit")
add_cmd("Display Tyres on MFD", "DISPLAY_TYRES", "Display", "Switch MFD display to tyre pressures/wear page", examples="display tyres;M F D tyres;show tyre mfd", aliases="show tyres;mfd tyres")
add_cmd("Display Temps on MFD", "DISPLAY_TEMPS", "Display", "Switch MFD display to temperatures page", examples="display temps;display temperatures;M F D temps", aliases="show temps;mfd temps")
add_cmd("Display Race Info on MFD", "DISPLAY_RACE_INFO", "Display", "Switch MFD display to race standings / info page", examples="display race info;M F D race info", aliases="show race info;mfd race info")
add_cmd("Display Standings on MFD", "DISPLAY_STANDINGS", "Display", "Switch MFD display to driver standings page", examples="display standings;M F D standings", aliases="show standings;mfd standings")
add_cmd("Display Penalties on MFD", "DISPLAY_PENALTIES", "Display", "Switch MFD display to penalties page", examples="display penalties;M F D penalties", aliases="show penalties;mfd penalties")
add_cmd("Display Next MFD Page", "DISPLAY_NEXT", "Display", "Cycle to the next MFD display screen", examples="display next;M F D next;next mfd page", aliases="next mfd;cycle mfd")

# 11. Overlays & Telemetry Charts
add_cmd("Show Overlay", "SHOW_OVERLAY", "Display", "Show the Crew Chief telemetry overlay", examples="show overlay;display overlay", aliases="open overlay;overlay on")
add_cmd("Hide Overlay", "HIDE_OVERLAY", "Display", "Hide the Crew Chief telemetry overlay", examples="hide overlay;close overlay", aliases="close overlay;overlay off")
add_cmd("Show All Overlays", "SHOW_All_OVERLAYS", "Display", "Unhide all active Crew Chief on-screen overlays", examples="show all overlays", aliases="show all overlays")
add_cmd("Show Console", "SHOW_CONSOLE", "Display", "Display the Crew Chief speech/log console window", examples="show console;open console", aliases="show console")
add_cmd("Hide Console", "HIDE_CONSOLE", "Display", "Hide the Crew Chief speech/log console window", examples="hide console;close console", aliases="hide console")
add_cmd("Show Chart", "SHOW_CHART", "Display", "Display telemetry comparison chart", examples="show chart;open chart", aliases="show chart;telemetry chart")
add_cmd("Hide Chart", "HIDE_CHART", "Display", "Hide telemetry comparison chart", examples="hide chart;close chart", aliases="hide chart;close chart")
add_cmd("Clear Chart", "CLEAR_CHART", "Display", "Reset telemetry chart traces", examples="new chart;clear chart", aliases="clear chart;new chart")
add_cmd("Clear Telemetry Data", "CLEAR_DATA", "Display", "Purge recorded telemetry data buffer", examples="clear data;reset telemetry", is_enabled=False)
add_cmd("Refresh Telemetry Chart", "REFRESH_CHART", "Display", "Refresh chart data view", examples="refresh chart;update chart", is_enabled=False)
add_cmd("Show Stacked Charts", "SHOW_STACKED_CHARTS", "Display", "Display charts in stacked multi-row layout", examples="show stacked charts", is_enabled=False)
add_cmd("Show Single Chart", "SHOW_SINGLE_CHART", "Display", "Display chart in single combined view", examples="show single chart", is_enabled=False)
add_cmd("Show Time Axis", "SHOW_TIME", "Display", "Render chart with elapsed time on X-axis", examples="show time;chart time axis", is_enabled=False)
add_cmd("Show Distance Axis", "SHOW_DISTANCE", "Display", "Render chart with lap distance on X-axis", examples="show distance;chart distance axis", is_enabled=False)
add_cmd("Show Subtitles", "SHOW_SUBTITLES", "Display", "Show spoken radio message subtitles overlay", examples="show subtitles;turn on subtitles", aliases="subtitles on;enable subtitles")
add_cmd("Hide Subtitles", "HIDE_SUBTITLES", "Display", "Hide spoken radio message subtitles overlay", examples="hide subtitles;close subtitles;turn off subtitles", aliases="subtitles off;disable subtitles")
add_cmd("Toggle VR Overlays", "TOGGLE_VR_OVERLAYS", "Display", "Toggle in-headset VR overlay visibility", examples="toggle VR overlays", is_enabled=False)
add_cmd("Show VR Settings", "SHOW_VR_SETTING", "Display", "Show VR position and scale adjustment settings", examples="show VR settings", is_enabled=False)
add_cmd("Hide VR Settings", "HIDE_VR_SETTING", "Display", "Hide VR position adjustment settings", examples="hide VR settings", is_enabled=False)

# Telemetry chart zoom/pan (disabled by default)
add_cmd("Chart Sector 1", "CHART_COMMAND_SHOW_SECTOR_1", "Display", "Focus chart on Sector 1", examples="show sector one", is_enabled=False)
add_cmd("Chart Sector 2", "CHART_COMMAND_SHOW_SECTOR_2", "Display", "Focus chart on Sector 2", examples="show sector two", is_enabled=False)
add_cmd("Chart Sector 3", "CHART_COMMAND_SHOW_SECTOR_3", "Display", "Focus chart on Sector 3", examples="show sector three", is_enabled=False)
add_cmd("Chart All Sectors", "CHART_COMMAND_SHOW_ALL_SECTORS", "Display", "Show full lap across all sectors on chart", examples="show all sectors", is_enabled=False)
add_cmd("Chart Zoom In", "CHART_COMMAND_ZOOM_IN", "Display", "Zoom in on chart data", examples="zoom in;chart zoom in", is_enabled=False)
add_cmd("Chart Zoom Out", "CHART_COMMAND_ZOOM_OUT", "Display", "Zoom out on chart data", examples="zoom out;chart zoom out", is_enabled=False)
add_cmd("Chart Reset Zoom", "CHART_COMMAND_RESET_ZOOM", "Display", "Reset zoom scale on chart", examples="reset zoom", is_enabled=False)
add_cmd("Chart Pan Left", "CHART_COMMAND_PAN_LEFT", "Display", "Pan chart view left", examples="pan left", is_enabled=False)
add_cmd("Chart Pan Right", "CHART_COMMAND_PAN_RIGHT", "Display", "Pan chart view right", examples="pan right", is_enabled=False)
add_cmd("Chart Show Last Lap", "CHART_COMMAND_SHOW_LAST_LAP", "Display", "Display telemetry from last completed lap", examples="show last lap", is_enabled=False)
add_cmd("Chart Show Next Lap", "CHART_COMMAND_SHOW_NEXT_LAP", "Display", "Display telemetry from next lap", examples="show next lap", is_enabled=False)
add_cmd("Chart Show Previous Lap", "CHART_COMMAND_SHOW_PREVIOUS_LAP", "Display", "Display telemetry from previous lap", examples="show previous lap", is_enabled=False)

# 12. Dynamic Opponent Phrases (Leader, Car Ahead, Car Behind)
add_cmd("Leader's Last Lap", "WHATS_THE_LEADER_LAST_LAP", "Timing", "Report last lap time set by the race leader", voice_line="what's the leader's last lap", examples="what was the leader's last lap;leader lap time;how fast was P1 last lap", aliases="leader lap;p1 lap")
add_cmd("Leader's Best Lap", "WHATS_THE_LEADER_BEST_LAP", "Timing", "Report fastest lap time set by the race leader", voice_line="what's the leader's best lap", examples="what's the leader's best lap;leader fastest lap;what is P1's best lap", aliases="leader best;p1 best")
add_cmd("Leader's iRating", "WHATS_THE_LEADER_IRATING", "Opponents", "Report iRating of the race leader", voice_line="what's the leader's eye rating", examples="what's the leader's irating;what rating does P1 have", aliases="leader irating")
add_cmd("Leader's License Class", "WHATS_THE_LEADER_LICENSE_CLASS", "Opponents", "Report safety license class of race leader", voice_line="what's the leader's license class", examples="what's the leader's license class", aliases="leader license")

add_cmd("Car Ahead's Last Lap", "WHATS_CAR_AHEAD_LAST_LAP", "Timing", "Report last lap time set by car directly ahead", voice_line="what's the car ahead's last lap", examples="what was the car ahead's last lap;how fast did the guy in front go;guy ahead lap time", aliases="car ahead lap;guy ahead lap")
add_cmd("Car Ahead's Best Lap", "WHATS_CAR_AHEAD_BEST_LAP", "Timing", "Report fastest lap time of competitor ahead", voice_line="what's the car ahead's best lap", examples="what's the car ahead's best lap;best lap of guy in front", aliases="car ahead best")
add_cmd("Car Ahead's iRating", "WHATS_CAR_AHEAD_IRATING", "Opponents", "Report iRating of competitor directly ahead", voice_line="what's the car ahead's eye rating", examples="what's the car ahead's irating;how high is the guy ahead's irating", aliases="ahead irating")
add_cmd("Car Ahead's License Class", "WHATS_CAR_AHEAD_LICENSE_CLASS", "Opponents", "Report safety license class of car ahead", voice_line="what's the car ahead's license class", examples="what's the car ahead's license class", aliases="ahead license")

add_cmd("Car Behind's Last Lap", "WHATS_CAR_BEHIND_LAST_LAP", "Timing", "Report last lap time set by car chasing behind", voice_line="what's the car behind's last lap", examples="what was the car behind's last lap;how fast is the guy behind going;guy behind lap time", aliases="car behind lap")
add_cmd("Car Behind's Best Lap", "WHATS_CAR_BEHIND_BEST_LAP", "Timing", "Report fastest lap time of competitor behind", voice_line="what's the car behind's best lap", examples="what's the car behind's best lap;best lap of guy behind", aliases="car behind best")
add_cmd("Car Behind's iRating", "WHATS_CAR_BEHIND_IRATING", "Opponents", "Report iRating of competitor behind", voice_line="what's the car behind's eye rating", examples="what's the car behind's irating;how good is the guy behind", aliases="behind irating")
add_cmd("Car Behind's License Class", "WHATS_CAR_BEHIND_LICENSE_CLASS", "Opponents", "Report safety license class of car behind", voice_line="what's the car behind's license class", examples="what's the car behind's license class", aliases="behind license")

add_cmd("Car Ahead Tyre Compound", "WHAT_TYRES_IS_CAR_AHEAD_ON", "Tyres", "Report tyre compound fitted on car ahead", voice_line="what tires is the guy ahead on", examples="what tires is the car ahead on;what tyres is the guy in front running", aliases="ahead tyres;ahead compound")
add_cmd("Car Behind Tyre Compound", "WHAT_TYRES_IS_CAR_BEHIND_ON", "Tyres", "Report tyre compound fitted on car behind", voice_line="what tires is the guy behind on", examples="what tires is the car behind on;what tyres is the guy behind running", aliases="behind tyres;behind compound")
add_cmd("Car Ahead Skill Assessment", "HOW_GOOD_IS_CAR_AHEAD", "Opponents", "Crew chief assessment of rank and skill of driver ahead", voice_line="how good is the driver in front", examples="how good is the guy ahead;how good is the driver in front", aliases="how good is guy ahead")
add_cmd("Car Behind Skill Assessment", "HOW_GOOD_IS_CAR_BEHIND", "Opponents", "Crew chief assessment of rank and skill of driver behind", voice_line="how good is the driver behind", examples="how good is the guy behind;how good is the driver behind", aliases="how good is guy behind")

# 13. Dynamic Parameterized Position & Driver Templates
add_cmd("Position Last Lap", "WHATS_POSITION_LAST_LAP", "Timing", "Report last lap time of driver in specific position (e.g. 'what's position 4's last lap')", voice_line="what's position {number}'s last lap", examples="what's P 4's last lap;what's position 2's last lap;what lap time did P3 do;how fast was position 5 last lap", aliases="p lap;position lap")
add_cmd("Position Best Lap", "WHATS_POSITION_BEST_LAP", "Timing", "Report best lap time of driver in specific position (e.g. 'what's position 3's best lap')", voice_line="what's position {number}'s best lap", examples="what's P 3's best lap;position 5 fastest lap;what is position 1's best lap", aliases="p best;position best")
add_cmd("Position iRating", "WHATS_POSITION_IRATING", "Opponents", "Report iRating of driver in specific position (e.g. 'what's position 2's eye rating')", voice_line="what's position {number}'s eye rating", examples="what's P 2's irating;what is position 4's eye rating", aliases="p irating;position irating")
add_cmd("Position License Class", "WHATS_POSITION_LICENSE_CLASS", "Opponents", "Report license class of driver in specific position", voice_line="what's position {number}'s license class", examples="what's P 5's license;what is position 2's license class", aliases="p license;position license")
add_cmd("Who is in Position", "WHOS_IN_POSITION", "Opponents", "Identify the driver in a specific race position (e.g. 'who's in position 5')", voice_line="who's in position {number}", examples="who's in P 3;who is in position 5;who is in fourth place;who's running second", aliases="who in position;who is in p")
add_cmd("Where is Position", "WHERE_IS_POSITION", "Opponents", "Report position and time gap to a specific position number", voice_line="where is position {number}", examples="where is P 4;where is position 2;gap to position 6", aliases="where is p;gap to p")
add_cmd("What Tyres is Position On", "WHAT_TYRES_IS_POSITION_ON", "Tyres", "Report tyre compound used by driver in specific position", voice_line="what tires is position {number} on", examples="what tires is P 3 on;what compound is position 4 running", aliases="p tires;position tires")
add_cmd("Where is Driver", "WHERE_IS_DRIVER", "Opponents", "Report position and gap to named driver (e.g. 'where is max')", voice_line="where is {driver}", examples="where is max;where is hamilton;gap to smith", aliases="find driver;track driver")
add_cmd("Driver Last Lap", "WHATS_DRIVER_LAST_LAP", "Timing", "Report last lap time of named driver", voice_line="what's {driver}'s last lap", examples="what was max's last lap;what's john's last lap", aliases="driver last lap")
add_cmd("Driver Best Lap", "WHATS_DRIVER_BEST_LAP", "Timing", "Report fastest lap of named driver", voice_line="what's {driver}'s best lap", examples="what's max's best lap;what is smith's fastest lap", aliases="driver best lap")
add_cmd("Set Alarm Clock", "SET_ALARM_CLOCK", "Session", "Set a reminder alarm timer in Crew Chief", voice_line="set alarm to {time}", examples="set alarm to 20 minutes;set alarm to 5 minutes;alarm me at 30 minutes", aliases="set alarm;alarm clock")
add_cmd("Clear Alarm Clock", "CLEAR_ALARM_CLOCK", "Session", "Cancel active alarm clock timer", voice_line="clear alarm clock", examples="clear alarm clock;clear alarms", aliases="clear alarm;cancel alarm")

# 14. Rally Pace Notes & Commands (disabled by default so they don't consume token budget)
rally_cmds = [
    ("Start Pace Notes Playback", "START_PACE_NOTES_PLAYBACK", "Rally", "Start automatic playback of recorded stage pace notes", "start pace notes", "start pace notes;play pace notes"),
    ("Stop Pace Notes Playback", "STOP_PACE_NOTES_PLAYBACK", "Rally", "Stop playing stage pace notes", "stop pace notes", "stop pace notes;mute pace notes"),
    ("Rally Earlier Callouts", "RALLY_EARLIER_CALLS", "Rally", "Instruct co-driver to give earlier corner calls", "earlier calls", "earlier calls;more notice"),
    ("Rally Later Callouts", "RALLY_LATER_CALLS", "Rally", "Instruct co-driver to give later corner calls", "later calls", "later calls"),
    ("Rally Corner Number First", "RALLY_CORNER_NUMBER_FIRST", "Rally", "Set co-driver style to announce severity number first", "corner number first", "corner number first;use corner number first"),
    ("Rally Corner Descriptions", "RALLY_CORNER_DECRIPTIONS", "Rally", "Set co-driver style to use descriptive calls (e.g. medium left)", "corner description", "corner description;use corner description"),
    ("Rally Corner Direction First", "RALLY_CORNER_DIRECTION_FIRST", "Rally", "Set co-driver style to announce direction first (e.g. left 4)", "corner direction first", "corner direction first"),
    ("Rally Start Recording Stage Notes", "RALLY_START_RECORDING_STAGE_NOTES", "Rally", "Begin recording custom pace notes during stage reconnaissance", "start stage reccy", "start stage reccy;start reccy;start recording pace notes"),
    ("Rally Finish Recording Stage Notes", "RALLY_FINISH_RECORDING_STAGE_NOTES", "Rally", "Finish and save recorded pace notes for this stage", "finish stage reccy", "finish stage reccy;finish reccy;finish recording pace notes"),
    ("Rally Start Reccy", "RALLY_START_RECE", "Rally", "Start stage reccy mode", "start stage reccy", "start stage reccy"),
    ("Rally Finish Reccy", "RALLY_FINISH_RECE", "Rally", "End stage reccy mode", "finish stage reccy", "finish stage reccy;end stage reccy"),
    ("Rally Correction", "RALLY_CORRECTION", "Rally", "Mark correction in active pace notes", "correction", "correction"),
    ("Rally Distance", "RALLY_DISTANCE", "Rally", "Pace note straight distance marker", "distance", "distance"),
    ("Rally Caution", "RALLY_CAUTION", "Rally", "Pace note single caution callout", "caution", "caution"),
    ("Rally Double Caution", "RALLY_DOUBLE_CAUTION", "Rally", "Pace note double caution callout", "double caution", "double caution"),
    ("Rally Triple Caution / Danger", "RALLY_DANGER", "Rally", "Pace note extreme danger / triple caution callout", "danger", "danger;triple caution"),
    ("Rally Hairpin", "RALLY_HAIRPIN", "Rally", "Pace note hairpin corner callout", "hairpin", "hairpin"),
    ("Rally Open Hairpin", "RALLY_OPEN_HAIRPIN", "Rally", "Pace note open hairpin callout", "open hairpin", "open hairpin"),
    ("Rally Square Corner", "RALLY_SQUARE", "Rally", "Pace note 90-degree square corner callout", "square", "square;square corner"),
    ("Rally Cut", "RALLY_CUT", "Rally", "Pace note cut corner callout", "cut", "cut"),
    ("Rally Don't Cut", "RALLY_DONT_CUT", "Rally", "Pace note don't cut corner callout", "don't cut", "don't cut;do not cut"),
    ("Rally Crest", "RALLY_CREST", "Rally", "Pace note crest callout", "crest", "crest;over crest"),
    ("Rally Jump", "RALLY_JUMP", "Rally", "Pace note jump callout", "jump", "jump;over jump"),
    ("Rally Big Jump", "RALLY_BIG_JUMP", "Rally", "Pace note big jump callout", "big jump", "big jump;over big jump"),
    ("Rally Bridge", "RALLY_BRIDGE", "Rally", "Pace note bridge crossing callout", "bridge", "bridge;over bridge"),
    ("Rally Water Splash / Ford", "RALLY_FORD", "Rally", "Pace note water splash callout", "ford", "ford;water splash"),
    ("Rally Keep Middle", "RALLY_KEEP_MIDDLE", "Rally", "Pace note stay in center of road", "keep middle", "keep middle;keep center"),
    ("Rally Keep Left", "RALLY_KEEP_LEFT", "Rally", "Pace note stay to left side of road", "keep left", "keep left"),
    ("Rally Keep Right", "RALLY_KEEP_RIGHT", "Rally", "Pace note stay to right side of road", "keep right", "keep right"),
    ("Rally Tarmac Surface", "RALLY_TARMAC", "Rally", "Pace note transition onto tarmac", "onto tarmac", "onto tarmac;tarmac"),
    ("Rally Gravel Surface", "RALLY_GRAVEL", "Rally", "Pace note transition onto gravel", "onto gravel", "onto gravel;gravel"),
    ("Rally Snow Surface", "RALLY_SNOW", "Rally", "Pace note transition onto snow", "onto snow", "onto snow;snow"),
    ("Rally Slippery Surface", "RALLY_SLIPPY", "Rally", "Pace note slippery road warning", "slippy", "slippy;slippery")
]

for r_name, r_id, r_cat, r_desc, r_voice, r_ex in rally_cmds:
    add_cmd(r_name, r_id, r_cat, r_desc, voice_line=r_voice, examples=r_ex, is_enabled=False)

# Write to CSV
fieldnames = ["Name", "CrewChiefCommandId", "Category", "Description", "VoiceLine", "Examples", "Aliases", "IsEnabled"]

with open(OUTPUT_CSV, "w", newline="", encoding="utf-8") as f:
    writer = csv.DictWriter(f, fieldnames=fieldnames, quoting=csv.QUOTE_MINIMAL)
    writer.writeheader()
    for cmd in commands:
        writer.writerow(cmd)

print(f"Successfully generated {len(commands)} commands in {OUTPUT_CSV}")

