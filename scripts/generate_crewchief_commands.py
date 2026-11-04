import csv
import io
import re

# Curated definition dictionary mapping CommandID -> (Friendly Name, Category, Description, Aliases, Additional Examples)
# We also parse the original voice lines from speech_recognition_config.txt and merge them cleanly,
# eliminating contradictory voice lines (e.g. removing general damage phrases from aero check).

CURATED_METADATA = {
    # --- TYRES ---
    "HOWS_MY_TYRE_WEAR": (
        "Tire Wear Report",
        "Tyres",
        "Reports percentage degradation and tread wear across all four tires.",
        "tire wear, tire status, tread wear",
        ["how's my tire wear", "what is my tire degradation", "check tire wear", "how much tread do I have left", "are my tires wearing out", "tire wear update"]
    ),
    "HOW_ARE_MY_TYRE_TEMPS": (
        "Tire Temperatures Overview",
        "Tyres",
        "Reports internal carcass temperatures across front and rear tires.",
        "tire temps, tire temperatures",
        ["how are my tire temps", "what are my tire temperatures", "check tire temperatures", "are my tires in the working window", "are the tires warmed up"]
    ),
    "WHAT_ARE_MY_TYRE_TEMPS": (
        "Tire Temperature Readings",
        "Tyres",
        "Reads exact temperature numbers for all four tire carcasses.",
        "exact tire temps, tire degrees",
        ["what are my tire temps", "give me tire temperatures", "read tire temps", "tire temperature check"]
    ),
    "WHAT_ARE_MY_TYRE_PRESSURES": (
        "Tire Pressures Overview",
        "Tyres",
        "Reports current running tire pressures in PSI or bar across all four wheels.",
        "tire pressures, pressures check",
        ["what are my tire pressures", "check tire pressures", "how are my pressures", "read tire pressures", "are my tire pressures optimal"]
    ),
    "HOW_ARE_MY_TYRE_PRESSURES": (
        "Tire Pressure Health Status",
        "Tyres",
        "Evaluates whether current tire pressures are within target hot pressure window.",
        "pressure health, pressure evaluation",
        ["how are my pressures", "how are my tire pressures", "are tire pressures ok", "check tire pressure status"]
    ),
    "HOW_ARE_MY_FRONT_TYRE_PRESSURES": (
        "Front Tire Pressures Check",
        "Tyres",
        "Reports running pressures specifically for the front axle tires.",
        "front pressures, front tire pressure",
        ["how are my front pressures", "check front tire pressures", "what are the front pressures", "front tire pressure status"]
    ),
    "HOW_ARE_MY_REAR_TYRE_PRESSURES": (
        "Rear Tire Pressures Check",
        "Tyres",
        "Reports running pressures specifically for the rear axle tires.",
        "rear pressures, rear tire pressure",
        ["how are my rear pressures", "check rear tire pressures", "what are the rear pressures", "rear tire pressure status"]
    ),
    "HOW_ARE_MY_TYRE_PRESSURES_RIGHT_NOW": (
        "Live Tire Pressures (Real-Time)",
        "Tyres",
        "Fetches instantaneous real-time live tire pressure telemetry.",
        "live pressures, instant pressures",
        ["how are my pressures right now", "what are my tire pressures right now", "instant tire pressures"]
    ),
    "HOW_ARE_MY_FRONT_TYRE_PRESSURES_RIGHT_NOW": (
        "Live Front Pressures (Real-Time)",
        "Tyres",
        "Fetches instantaneous live front tire pressure telemetry.",
        "live front pressures",
        ["how are my front pressures right now", "check front tire pressures right now"]
    ),
    "HOW_ARE_MY_REAR_TYRE_PRESSURES_RIGHT_NOW": (
        "Live Rear Pressures (Real-Time)",
        "Tyres",
        "Fetches instantaneous live rear tire pressure telemetry.",
        "live rear pressures",
        ["how are my rear pressures right now", "check rear tire pressures right now"]
    ),
    "WHAT_TYRES_AM_I_ON": (
        "Active Tire Compound Check",
        "Tyres",
        "Identifies the currently fitted tire compound (e.g. Slick, Wet, Soft, Medium, Hard).",
        "tire compound, current compound, fitted tires",
        ["what tires am I on", "what compound am I running", "which tire set is on the car", "am I on slicks or wets", "what rubber do I have on"]
    ),
    "HOW_LONG_WILL_THESE_TYRES_LAST": (
        "Tire Life Expectancy Estimate",
        "Tyres",
        "Estimates how many more laps or minutes the current tire set can survive before critical degradation.",
        "tire life, tire mileage, stint tire life",
        ["how long will the tires last", "how many laps left on these tires", "how long on these tires", "can these tires make it to the end", "will tires last until pit window"]
    ),
    "WHAT_ARE_THE_RELATIVE_TYRE_PERFORMANCES": (
        "Tire Compound Pace Comparison",
        "Tyres",
        "Compares pace deltas and durability between available tire compounds.",
        "tire pace comparison, compound delta",
        ["compare tire compounds", "what are the tire speeds", "give me tire pace differences", "what is the difference between soft and medium", "how much faster are softs"]
    ),
    "HOW_MANY_LAPS_ON_TYRE_SET": (
        "Laps on Current Tire Set",
        "Tyres",
        "Reports the total completed lap count on the active tire set (ACC).",
        "laps on tires, tire age",
        ["how many laps on this tire set", "how old are these tires", "how many laps have these tires done", "check tire age"]
    ),
    "WHAT_ARE_MY_LEFT_FRONT_SURFACE_TEMPS": (
        "Left-Front Tire Surface Temperatures",
        "Tyres",
        "Reports inner, middle, and outer surface tread temperatures for the left front tire.",
        "LF temps, left front surface",
        ["what are my left front surface temps", "left front tire temperatures", "check LF surface temp", "left front inner middle outer temps"]
    ),
    "WHAT_ARE_MY_RIGHT_FRONT_SURFACE_TEMPS": (
        "Right-Front Tire Surface Temperatures",
        "Tyres",
        "Reports inner, middle, and outer surface tread temperatures for the right front tire.",
        "RF temps, right front surface",
        ["what are my right front surface temps", "right front tire temperatures", "check RF surface temp", "right front inner middle outer temps"]
    ),
    "WHAT_ARE_MY_LEFT_REAR_SURFACE_TEMPS": (
        "Left-Rear Tire Surface Temperatures",
        "Tyres",
        "Reports inner, middle, and outer surface tread temperatures for the left rear tire.",
        "LR temps, left rear surface",
        ["what are my left rear surface temps", "left rear tire temperatures", "check LR surface temp", "left rear inner middle outer temps"]
    ),
    "WHAT_ARE_MY_RIGHT_REAR_SURFACE_TEMPS": (
        "Right-Rear Tire Surface Temperatures",
        "Tyres",
        "Reports inner, middle, and outer surface tread temperatures for the right rear tire.",
        "RR temps, right rear surface",
        ["what are my right rear surface temps", "right rear tire temperatures", "check RR surface temp", "right rear inner middle outer temps"]
    ),
    "HOWS_MY_FRONT_CAMBER": (
        "Front Axle Dynamic Camber",
        "Tyres",
        "Evaluates front dynamic camber angle based on thermal spread across the tread.",
        "front camber check",
        ["how's my front camber", "check front camber", "is front camber optimal"]
    ),
    "HOWS_MY_REAR_CAMBER": (
        "Rear Axle Dynamic Camber",
        "Tyres",
        "Evaluates rear dynamic camber angle based on thermal spread across the tread.",
        "rear camber check",
        ["how's my rear camber", "check rear camber", "is rear camber optimal"]
    ),
    "HOWS_MY_LEFT_FRONT_CAMBER": (
        "Left-Front Camber Check",
        "Tyres",
        "Evaluates dynamic camber angle for the left-front wheel.",
        "LF camber",
        ["how's my left front camber", "check left front camber"]
    ),
    "HOWS_MY_RIGHT_FRONT_CAMBER": (
        "Right-Front Camber Check",
        "Tyres",
        "Evaluates dynamic camber angle for the right-front wheel.",
        "RF camber",
        ["how's my right front camber", "check right front camber"]
    ),
    "HOWS_MY_LEFT_REAR_CAMBER": (
        "Left-Rear Camber Check",
        "Tyres",
        "Evaluates dynamic camber angle for the left-rear wheel.",
        "LR camber",
        ["how's my left rear camber", "check left rear camber"]
    ),
    "HOWS_MY_RIGHT_REAR_CAMBER": (
        "Right-Rear Camber Check",
        "Tyres",
        "Evaluates dynamic camber angle for the right-rear wheel.",
        "RR camber",
        ["how's my right rear camber", "check right rear camber"]
    ),
    "HOWS_MY_FRONT_CAMBER_RIGHT_NOW": (
        "Live Front Camber (Real-Time)",
        "Tyres",
        "Reports instantaneous live front camber telemetry.",
        "live front camber",
        ["how's my front camber right now", "instant front camber"]
    ),
    "HOWS_MY_REAR_CAMBER_RIGHT_NOW": (
        "Live Rear Camber (Real-Time)",
        "Tyres",
        "Reports instantaneous live rear camber telemetry.",
        "live rear camber",
        ["how's my rear camber right now", "instant rear camber"]
    ),
    "HOWS_MY_LEFT_FRONT_CAMBER_RIGHT_NOW": (
        "Live Left-Front Camber (Real-Time)",
        "Tyres",
        "Reports instantaneous live left-front camber telemetry.",
        "live LF camber",
        ["how's my left front camber right now"]
    ),
    "HOWS_MY_RIGHT_FRONT_CAMBER_RIGHT_NOW": (
        "Live Right-Front Camber (Real-Time)",
        "Tyres",
        "Reports instantaneous live right-front camber telemetry.",
        "live RF camber",
        ["how's my right front camber right now"]
    ),
    "HOWS_MY_LEFT_REAR_CAMBER_RIGHT_NOW": (
        "Live Left-Rear Camber (Real-Time)",
        "Tyres",
        "Reports instantaneous live left-rear camber telemetry.",
        "live LR camber",
        ["how's my left rear camber right now"]
    ),
    "HOWS_MY_RIGHT_REAR_CAMBER_RIGHT_NOW": (
        "Live Right-Rear Camber (Real-Time)",
        "Tyres",
        "Reports instantaneous live right-rear camber telemetry.",
        "live RR camber",
        ["how's my right rear camber right now"]
    ),

    # --- BRAKES ---
    "HOWS_MY_BRAKES": (
        "Brake Wear & Health Status",
        "Brakes",
        "Reports overall brake condition, disc pad wear, and fade levels.",
        "brake status, brake wear, brake health",
        ["how are my brakes", "how's my brakes", "check brake condition", "are the brakes holding up", "how much brake pad do I have left", "brake health check"]
    ),
    "HOW_ARE_MY_BRAKE_TEMPS": (
        "Brake Temperature Overview",
        "Brakes",
        "Evaluates whether brake discs and calipers are within optimal operating heat window.",
        "brake temps, brake heat",
        ["how are my brake temps", "how are my brake temperatures", "are the brakes too hot", "check brake temps", "are the brakes in the operating window"]
    ),
    "WHAT_ARE_MY_BRAKE_TEMPS": (
        "Brake Temperature Readings",
        "Brakes",
        "Reads exact disc temperature numbers in Celsius or Fahrenheit for all four corners.",
        "exact brake temps, brake readings",
        ["what are my brake temps", "what are my brake temperatures", "give me brake disc temperatures", "read brake temps"]
    ),

    # --- ENGINE & POWERTRAIN ---
    "HOWS_MY_ENGINE": (
        "Engine Health Status",
        "Engine",
        "Reports internal engine integrity, rev-limiter damage, and power loss.",
        "engine status, motor health",
        ["how's my engine", "is the engine running ok", "check engine health", "do we have any engine damage", "engine diagnostic check"]
    ),
    "HOW_ARE_MY_ENGINE_TEMPS": (
        "Engine Temperatures Overview",
        "Engine",
        "Evaluates engine thermal state across water radiator and oil cooling systems.",
        "engine temps, motor heat",
        ["how are my engine temps", "is the engine overheating", "are engine temps ok", "how are the motor temperatures", "engine temperature check"]
    ),
    "WHAT_ARE_MY_ENGINE_TEMPS": (
        "Engine Temperature Readings",
        "Engine",
        "Reads exact numerical degrees for both coolant and oil temperatures.",
        "engine temp readings",
        ["what are my engine temps", "what are my engine temperatures", "give me engine temperatures", "read coolant and oil temps"]
    ),
    "WHAT_IS_MY_WATER_TEMP": (
        "Water / Coolant Temperature Check",
        "Engine",
        "Reports current radiator water coolant temperature reading.",
        "water temp, coolant temp",
        ["what's my water temp", "what is my water temperature", "check water temp", "coolant temperature reading"]
    ),
    "WHAT_IS_MY_OIL_TEMP": (
        "Engine Oil Temperature Check",
        "Engine",
        "Reports current engine oil temperature reading.",
        "oil temp, oil temperature",
        ["what's my oil temp", "what is my oil temperature", "check oil temp", "oil temperature reading"]
    ),
    "HOWS_MY_TRANSMISSION": (
        "Transmission & Gearbox Health",
        "Engine",
        "Reports gearbox condition, missed shifts wear, clutch wear, and drivetrain damage.",
        "transmission status, gearbox health",
        ["how's my transmission", "how's my gearbox", "is the gearbox ok", "check transmission health", "any gearbox damage", "drivetrain status"]
    ),
    "HOWS_MY_BATTERY": (
        "Hybrid / Battery State of Charge",
        "Engine",
        "Reports hybrid battery charge level, ERS deployment status, or electrical system state.",
        "battery status, hybrid charge, ERS level",
        ["how's my battery", "how's my battery level", "what's my state of charge", "how much hybrid deploy do I have left", "ERS battery status"]
    ),
    "HOWS_MY_SUSPENSION": (
        "Suspension & Alignment Health",
        "Engine",
        "Reports suspension geometry, damper condition, and bent tie-rod / alignment damage.",
        "suspension status, alignment check",
        ["how's my suspension", "is the suspension damaged", "check suspension alignment", "did that curb bend my suspension", "steering alignment check"]
    ),

    # --- DAMAGE & VEHICLE STATUS ---
    "DAMAGE_REPORT": (
        "Comprehensive Vehicle Damage Report",
        "Damage",
        "Provides a full health summary covering aero, suspension, bodywork, steering, and engine damage.",
        "damage report, full damage, vehicle health",
        ["damage report", "how's my car", "is my car ok", "give me a full damage report", "how bad is the damage", "what's broken on the car", "car health check"]
    ),
    "HOWS_MY_AERO": (
        "Aerodynamics & Wing Damage Check",
        "Damage",
        "Specifically reports downforce loss, front splitter damage, and rear wing structural integrity.",
        "aero check, wing damage, downforce check",
        ["how's my aero", "do I have aero damage", "how is front wing and splitter", "check rear wing", "did I lose downforce", "is the aero compromised"]
    ),
    "CAR_STATUS": (
        "Overall Car Health Summary",
        "Damage",
        "Concise summary of general vehicle integrity, mechanical warnings, and fluid levels.",
        "car status, vehicle condition",
        ["car status", "give me car status", "how is the car doing", "overall car condition"]
    ),
    "STATUS": (
        "Full Race Cockpit Briefing",
        "Damage",
        "Full comprehensive briefing covering position, gaps, fuel remaining, tire state, and car health.",
        "full status, complete briefing, sitrep",
        ["full status", "update me", "give me full update", "full race briefing", "sitrep", "full cockpit status"]
    ),

    # --- FUEL & STRATEGY ---
    "WHATS_MY_FUEL_LEVEL": (
        "Current Fuel Remaining Check",
        "Fuel",
        "Reports exact amount of fuel currently remaining in the tank (liters or gallons).",
        "fuel level, fuel in tank, fuel remaining",
        ["what's my fuel level", "how much fuel is in the tank", "how many liters do I have left", "check fuel remaining", "fuel level check"]
    ),
    "HOWS_MY_FUEL": (
        "Fuel Situation Status",
        "Fuel",
        "General assessment of whether current fuel load is on target for the planned stint.",
        "fuel status, fuel situation",
        ["how's my fuel", "how's my fuel level", "how are we on fuel", "is fuel ok for this stint"]
    ),
    "WHATS_MY_FUEL_USAGE": (
        "Average Fuel Consumption Check",
        "Fuel",
        "Reports average fuel consumption per lap (e.g. 3.2 liters per lap).",
        "fuel usage, fuel consumption, burn rate",
        ["what's my fuel usage", "what's my fuel consumption", "how much fuel per lap am I using", "what is our burn rate per lap", "average fuel use"]
    ),
    "HOW_MUCH_FUEL_TO_END_OF_RACE": (
        "Fuel Required to Finish Race",
        "Fuel",
        "Calculates exact fuel needed to reach the checkered flag without running dry.",
        "fuel to finish, fuel to end, required fuel",
        ["how much fuel to the end", "how much fuel to finish the race", "how much fuel do we need to make the finish", "will we make the end on this fuel", "fuel required to checkered flag"]
    ),
    "CALCULATE_FUEL_FOR": (
        "Fuel Calculator for Laps or Time",
        "Fuel",
        "Calculates required fuel volume for a user-specified number of laps or duration.",
        "calculate fuel, fuel calc",
        ["calculate fuel for next stint", "calculate fuel for 30 minutes", "how much fuel do I need for 15 laps", "calculate fuel load"]
    ),

    # --- TIMING, DELTAS & GAPS ---
    "WHATS_MY_GAP_IN_FRONT": (
        "Gap to Car Ahead",
        "Gaps",
        "Reports current time interval / delta to the car directly in front of the driver.",
        "gap ahead, gap in front, interval ahead",
        ["what's the gap ahead", "what's the gap in front", "what's my gap ahead", "how far ahead is the car in front", "what is the delta to the guy in front", "gap to car ahead"]
    ),
    "WHATS_MY_GAP_BEHIND": (
        "Gap to Car Behind",
        "Gaps",
        "Reports current time interval / delta to the pursuing car directly behind the driver.",
        "gap behind, interval behind, gap to car behind",
        ["what's the gap behind", "how far behind is the guy behind", "what's my gap behind", "is anyone catching me from behind", "what is the delta behind"]
    ),
    "WHAT_WAS_MY_LAST_LAP_TIME": (
        "Last Completed Lap Time",
        "Timing",
        "Reports lap time recorded on the most recently completed lap.",
        "last lap, last lap time",
        ["what was my last lap time", "what was my last lap", "last lap time", "what did I do that lap", "what did I run last lap"]
    ),
    "WHATS_MY_BEST_LAP_TIME": (
        "Personal Best Lap Time",
        "Timing",
        "Reports driver's fastest personal lap time set during the current session.",
        "best lap, personal best, PB lap",
        ["what's my best lap time", "what is my PB", "best lap time", "what's my fastest lap today", "give me my personal best"]
    ),
    "WHATS_THE_FASTEST_LAP_TIME": (
        "Overall Session Fastest Lap Time",
        "Timing",
        "Reports the absolute fastest lap time set by any driver in the current session.",
        "fastest lap, session best, purple lap",
        ["what's the fastest lap", "what's the fastest lap time of the race", "who has purple lap", "what is the outright best lap"]
    ),
    "WHAT_ARE_MY_SECTOR_TIMES": (
        "Current / Last Sector Split Times",
        "Timing",
        "Reports Sector 1, Sector 2, and Sector 3 split times for the current or previous lap.",
        "sector times, split times, all sectors",
        ["what are my sector times", "give me sector splits", "how were my sectors", "read sector times", "split times check"]
    ),
    "WHATS_MY_LAST_SECTOR_TIME": (
        "Last Sector Split Time",
        "Timing",
        "Reports split time and delta for the single sector just crossed.",
        "last sector, recent sector split",
        ["what's my last sector time", "how was that sector", "what did I do in sector two", "sector time check"]
    ),
    "HOWS_MY_PACE": (
        "General Race Pace Evaluation",
        "Timing",
        "Compares driver's average pace against the immediate competitors in the field.",
        "pace check, race pace",
        ["how's my pace", "am I on the pace", "how does my pace look compared to the pack", "am I fast enough", "pace evaluation"]
    ),
    "HOWS_MY_SELF_PACE": (
        "Pace vs Personal Best (Delta Best)",
        "Timing",
        "Reports current pace delta relative to driver's personal best reference lap.",
        "self pace, delta best, personal pace",
        ["how's my personal pace", "how's my delta best", "how am I doing against my best lap", "how's my self gap"]
    ),
    "WHERE_AM_I_FASTER": (
        "Track Attack Analysis (Where Driver Gained)",
        "Timing",
        "Identifies track sectors or corners where driver is gaining time over competitor.",
        "where am I faster, where to attack",
        ["where am I faster", "where should I attack", "where can I attack", "where am I gaining on him", "where am I quicker"]
    ),
    "WHERE_AM_I_SLOWER": (
        "Track Defense Analysis (Where Driver Lost)",
        "Timing",
        "Identifies track sectors or corners where competitor is faster and driver needs to defend.",
        "where am I slower, where to defend",
        ["where am I slower", "where should I defend", "where is he faster", "where will he attack me", "where am I losing time"]
    ),

    # --- SESSION, POSITION & OPPONENTS ---
    "WHATS_MY_POSITION": (
        "Current Race Position Check",
        "Session",
        "Reports driver's active race or qualifying standing / position in class.",
        "position check, race position, P check",
        ["what's my position", "what place am I in", "what position are we running", "where are we in the race", "what's my P"]
    ),
    "WHATS_MY_EXPECTED_FINISH_POSITION": (
        "Projected Finish Position",
        "Session",
        "Projects estimated finish position based on current pace, fuel, and opponent pit stops.",
        "expected position, projected finish",
        ["what's my expected finish position", "where will we finish", "projected finish position", "what's my expected position"]
    ),
    "HOW_LONGS_LEFT": (
        "Session Time / Laps Remaining",
        "Session",
        "Reports remaining race time or number of laps left before the checkered flag.",
        "time left, laps left, race clock",
        ["how long's left", "how many laps left", "how much time remaining in the race", "how many laps to go", "how long till checkered flag"]
    ),
    "SESSION_STATUS": (
        "Session Status & Track Overview",
        "Session",
        "Overview of session phase, leader lap count, weather trend, and safety car status.",
        "session status, race status",
        ["session status", "race status", "how is the session looking", "what's happening in the race"]
    ),
    "WHOS_LEADING": (
        "Race Leader Identification",
        "Session",
        "Reports driver name and car number of the overall race leader.",
        "leader name, who is leading",
        ["who's leading", "who's in the lead", "who is the race leader", "who's in first place", "who is leading the race"]
    ),
    "WHOS_IN_FRONT_IN_THE_RACE": (
        "Driver Ahead in Standings",
        "Session",
        "Identifies the driver directly ahead in the official race classification.",
        "who is ahead, driver ahead",
        ["who's in front in the race", "who is ahead in the race", "who am I racing ahead", "who is in front of me in standings"]
    ),
    "WHOS_BEHIND_IN_THE_RACE": (
        "Driver Behind in Standings",
        "Session",
        "Identifies the driver directly behind in the official race classification.",
        "who is behind, driver behind",
        ["who's behind in the race", "who is behind me in the race", "who is chasing me", "who is behind in standings"]
    ),
    "WHOS_IN_FRONT_ON_TRACK": (
        "Car Directly Ahead on Track",
        "Session",
        "Identifies the physical car on track in front of driver (including lap traffic).",
        "car ahead on track, who is in front",
        ["who's in front on track", "who's ahead on track", "who is that car in front of me", "is the car ahead on the lead lap"]
    ),
    "WHOS_BEHIND_ON_TRACK": (
        "Car Directly Behind on Track",
        "Session",
        "Identifies the physical car on track behind driver (including lead lap cars).",
        "car behind on track",
        ["who's behind on track", "who is right behind me on track", "is that car behind lapping me"]
    ),
    "IS_CAR_AHEAD_MY_CLASS": (
        "Is Car Ahead Same Vehicle Class",
        "Session",
        "Determines whether the vehicle directly ahead is competing in the same class (multiclass).",
        "car ahead class check, same class ahead",
        ["is the car ahead in my class", "is the guy ahead the same class as me", "are we racing the car in front", "is that car in my category"]
    ),
    "IS_CAR_BEHIND_MY_CLASS": (
        "Is Car Behind Same Vehicle Class",
        "Session",
        "Determines whether the vehicle directly behind is competing in the same class (multiclass).",
        "car behind class check, same class behind",
        ["is the car behind in my class", "is the guy behind the same class as me", "is the car chasing me my class"]
    ),
    "WHAT_CLASS_IS_CAR_AHEAD": (
        "Class of Car Ahead",
        "Session",
        "Reports the category/class designation of the car ahead (e.g. GT3, GT4, Cup, LMP2).",
        "class of car ahead, car ahead category",
        ["what class is the car ahead", "what category is the guy in front", "what car class is ahead"]
    ),
    "WHAT_CLASS_IS_CAR_BEHIND": (
        "Class of Car Behind",
        "Session",
        "Reports the category/class designation of the car behind (e.g. GT3, GT4, Cup, LMP2).",
        "class of car behind, car behind category",
        ["what class is the car behind", "what category is the guy behind", "what car class is behind me"]
    ),
    "WHATS_THE_AIR_TEMP": (
        "Ambient Air Temperature",
        "Session",
        "Reports current ambient weather temperature in Celsius or Fahrenheit.",
        "air temp, weather temp, ambient temperature",
        ["what's the air temp", "what's the air temperature", "how warm is it outside", "check ambient temp", "current weather temperature"]
    ),
    "WHATS_THE_TRACK_TEMP": (
        "Track Surface Temperature",
        "Session",
        "Reports current asphalt track surface temperature.",
        "track temp, track temperature, asphalt temp",
        ["what's the track temp", "what's the track temperature", "how hot is the asphalt", "check track surface temp", "is track temp changing"]
    ),
    "WHATS_THE_TIME": (
        "Current Wall Clock Time",
        "Session",
        "Reports real-world local wall clock time.",
        "clock time, real time, what time is it",
        ["what's the time", "what time is it", "tell me real world time", "what's the clock time"]
    ),
    "WHATS_MY_IRATING": (
        "iRating Check",
        "Session",
        "Reports driver's current active iRacing iRating score.",
        "irating, driver rating",
        ["what's my irating", "check my irating", "what is my eye rating"]
    ),
    "WHATS_MY_LICENSE_CLASS": (
        "License Safety Class Check",
        "Session",
        "Reports driver's license class and safety rating (e.g. Class A 3.85).",
        "license class, safety rating",
        ["what's my license class", "what is my safety rating", "check license rank"]
    ),
    "WHATS_MY_RANK": (
        "Driver Ranking Query",
        "Session",
        "Reports driver leaderboard ranking / server rank.",
        "driver rank, server rank",
        ["what's my rank", "what is my ranking"]
    ),
    "WHATS_MY_RATING": (
        "Driver Performance Rating",
        "Session",
        "Reports driver competitive rating score.",
        "driver rating",
        ["what's my rating", "what is my driver rating"]
    ),
    "WHATS_MY_REPUTATION": (
        "Driver Safety Reputation",
        "Session",
        "Reports safety reputation and sportsmanship score.",
        "reputation check",
        ["what's my reputation", "check driver reputation"]
    ),
    "HOW_MANY_INCIDENT_POINTS": (
        "Incident Points Accumulation",
        "Session",
        "Reports current incident point total accumulated in session (iRacing).",
        "incident count, incident points",
        ["how many incident points do I have", "what's my incident count", "how many incidents do I have", "check incident points"]
    ),
    "WHATS_THE_INCIDENT_LIMIT": (
        "Incident Disqualification Limit",
        "Session",
        "Reports maximum allowable incident points before drive-through penalty or disqualification.",
        "incident limit, incident cap",
        ["what's the incident limit", "how many incident points until DQ", "what is the incident cap"]
    ),
    "WHATS_THE_SOF": (
        "Strength of Field (SoF)",
        "Session",
        "Reports overall Strength of Field rating for the current race split.",
        "strength of field, SOF rating",
        ["what's the sof", "what is the strength of field", "how high is the sof for this race"]
    ),
    "STOP_WATCHING_ALL": (
        "Stop Monitoring Opponents",
        "Session",
        "Cancels and clears all active monitored opponent driver trackings.",
        "stop monitoring, cancel rivals",
        ["stop monitoring all", "cancel monitored drivers", "stop watching opponents"]
    ),

    # --- PITSTOP - STRATEGY & ACTIONS ---
    "DO_I_HAVE_A_MANDATORY_PIT_STOP": (
        "Mandatory Pit Stop Requirement Check",
        "Pitstop",
        "Verifies whether the driver must make a mandatory pit stop and if the pit window is open.",
        "mandatory pit, pit window check",
        ["do I have a mandatory pit stop", "do I have to pit", "do I need to make a pit stop", "is there a mandatory stop in this race", "is the pit window open"]
    ),
    "IS_MY_PIT_BOX_OCCUPIED": (
        "Pit Stall Occupancy Status",
        "Pitstop",
        "Checks if another car or teammate is currently occupying driver's pit box.",
        "pit box occupied, pit stall free",
        ["is my pit box occupied", "is my pit stall clear", "can I come in now", "is anyone in my pit box"]
    ),
    "WHATS_PITLANE_SPEED_LIMIT": (
        "Pit Lane Speed Limit Check",
        "Pitstop",
        "Reports regulated pit lane speed limit (e.g. 50 km/h or 60 km/h / 37 mph).",
        "pit speed limit, pitlane speed",
        ["what's the pitlane speed limit", "what's the pit speed limit", "how fast can I go in pit lane", "pit speed check"]
    ),
    "PLAY_POST_PIT_POSITION_ESTIMATE": (
        "Estimated Track Rejoin Position After Pit",
        "Pitstop",
        "Projects what position and track traffic driver will rejoin in after completing a pit stop now.",
        "pit exit position, position after stop",
        ["where will I be after a stop", "estimate pit exit positions", "what will happen if I pit now", "where will I come out after pitting"]
    ),
    "PRACTICE_PIT_STOP": (
        "Practice Pit Stop Timer Benchmark",
        "Pitstop",
        "Times and benchmarks the current pit stop entry, service, and pit exit time.",
        "time pitstop, benchmark stop",
        ["time this stop", "practice pitstop", "time this pitstop", "benchmark pitstop"]
    ),
    "WHAT_ARE_THE_PIT_ACTIONS": (
        "Review Scheduled Pit Service Actions",
        "Pitstop",
        "Reads out all currently queued pit stop actions (fuel liters, tire change, repairs).",
        "pit actions plan, pit plan check",
        ["what are the pit actions", "what's the pitstop plan", "what are we doing at the pit stop", "read pit actions"]
    ),
    "PIT_STOP_ADD": (
        "Queue Refueling at Next Stop",
        "Pitstop",
        "Commands the crew to add fuel during the upcoming pit stop.",
        "pit add fuel, box add fuel",
        ["pitstop add fuel", "box add fuel", "add fuel at this stop"]
    ),
    "PIT_STOP_FUEL_TO_THE_END": (
        "Set Pit Fuel to Finish Race",
        "Pitstop",
        "Calculates and automatically programs the exact fuel quantity needed to complete the race.",
        "box fuel to end, pit fuel to finish",
        ["pitstop fuel to the end", "fuel to the end of the race", "box fuel to the end", "calculate and add fuel to finish"]
    ),
    "PIT_STOP_REFUEL": (
        "Enable Pit Refueling",
        "Pitstop",
        "Ensures refueling is enabled in the pit menu for the upcoming stop.",
        "box refuel, pit refuel",
        ["pitstop refuel", "box refuel", "make sure we take fuel at this stop"]
    ),
    "PIT_STOP_DONT_REFUEL": (
        "Disable Pit Refueling (Splash & Go / Tires Only)",
        "Pitstop",
        "Unchecks refueling in the pit menu to avoid unnecessary pit time.",
        "box don't refuel, no fuel",
        ["pitstop don't refuel", "box don't refuel", "no fuel at this stop", "don't put fuel in"]
    ),
    "PIT_STOP_CLEAR_FUEL": (
        "Clear Pit Fuel Setting",
        "Pitstop",
        "Sets scheduled fuel addition to 0 liters.",
        "clear pit fuel",
        ["pitstop clear fuel", "box clear fuel", "clear scheduled fuel"]
    ),
    "PIT_STOP_CLEAR_ALL": (
        "Clear All Queued Pit Actions",
        "Pitstop",
        "Deselects all fuel, tires, and repair work in the pit menu.",
        "clear pit menu, cancel pit actions",
        ["pitstop clear all", "box clear all", "cancel all pit service", "uncheck all pit items"]
    ),
    "PIT_STOP_TEAROFF": (
        "Replace Windshield Tear-off at Stop",
        "Pitstop",
        "Schedules a visor or windshield tearoff removal during the pit stop.",
        "tearoff, windscreen tearoff",
        ["pitstop tearoff", "box tearoff", "pitstop windscreen", "clean windscreen at stop"]
    ),
    "PIT_STOP_CLEAR_WIND_SCREEN": (
        "Cancel Windshield Tear-off",
        "Pitstop",
        "Cancels tearoff replacement to save pit lane time.",
        "clear tearoff",
        ["pitstop clear tearoff", "box clear windscreen", "don't touch the windscreen"]
    ),
    "PIT_STOP_FAST_REPAIR": (
        "Use Fast Repair at Next Stop",
        "Pitstop",
        "Activates the instant fast repair option during the upcoming pit stop (iRacing).",
        "fast repair, instant repair",
        ["pitstop fast repair", "box fast repair", "use fast repair"]
    ),
    "PIT_STOP_CLEAR_FAST_REPAIR": (
        "Cancel Fast Repair",
        "Pitstop",
        "Disables fast repair option to save it for later in the race.",
        "clear fast repair",
        ["pitstop clear fast repair", "box clear fast repair", "save fast repair"]
    ),

    # --- PITSTOP - TIRE SERVICE ---
    "PIT_STOP_CHANGE_ALL_TYRES": (
        "Change All Four Tires at Stop",
        "Pitstop",
        "Commands pit crew to replace all four tires during the pit stop.",
        "change four tires, full tire change",
        ["pitstop change all tires", "box change all tires", "replace all four tires at this stop", "full set of tires"]
    ),
    "PIT_STOP_CHANGE_TYRES": (
        "Change Tires at Stop",
        "Pitstop",
        "Commands pit crew to perform standard tire replacement.",
        "box change tires, pit change tires",
        ["pitstop change tires", "box change tires", "change tires at next stop"]
    ),
    "PIT_STOP_CLEAR_TYRES": (
        "Do Not Change Tires at Stop",
        "Pitstop",
        "Deselects tire changes in pit menu for fuel-only or quick stop.",
        "keep current tires, no tire change",
        ["pitstop clear tires", "box don't change tires", "pitstop don't change tires", "leave the tires on", "no tire change"]
    ),
    "PIT_STOP_CHANGE_FRONT_LEFT_TYRE": (
        "Change Left-Front Tire Only",
        "Pitstop",
        "Commands pit crew to change only the left-front tire at the stop.",
        "change LF tire",
        ["pitstop change left front tire", "box change left front tire", "change LF tire only"]
    ),
    "PIT_STOP_CHANGE_FRONT_RIGHT_TYRE": (
        "Change Right-Front Tire Only",
        "Pitstop",
        "Commands pit crew to change only the right-front tire at the stop.",
        "change RF tire",
        ["pitstop change right front tire", "box change right front tire", "change RF tire only"]
    ),
    "PIT_STOP_CHANGE_REAR_LEFT_TYRE": (
        "Change Left-Rear Tire Only",
        "Pitstop",
        "Commands pit crew to change only the left-rear tire at the stop.",
        "change LR tire",
        ["pitstop change left rear tire", "box change left rear tire", "change LR tire only"]
    ),
    "PIT_STOP_CHANGE_REAR_RIGHT_TYRE": (
        "Change Right-Rear Tire Only",
        "Pitstop",
        "Commands pit crew to change only the right-rear tire at the stop.",
        "change RR tire",
        ["pitstop change right rear tire", "box change right rear tire", "change RR tire only"]
    ),
    "PIT_STOP_CHANGE_LEFT_SIDE_TYRES": (
        "Change Left-Side Tires Only",
        "Pitstop",
        "Commands pit crew to replace both left-side tires (LF & LR).",
        "change left side tires",
        ["pitstop change left side tires", "box change left side tires", "change lefts only"]
    ),
    "PIT_STOP_CHANGE_RIGHT_SIDE_TYRES": (
        "Change Right-Side Tires Only",
        "Pitstop",
        "Commands pit crew to replace both right-side tires (RF & RR).",
        "change right side tires",
        ["pitstop change right side tires", "box change right side tires", "change rights only"]
    ),
    "PIT_STOP_CHANGE_FRONT_TYRES": (
        "Change Front Tires Only",
        "Pitstop",
        "Commands pit crew to replace only the front axle tires.",
        "change fronts only",
        ["pitstop change front tires only", "box change front tires only", "change front tires"]
    ),
    "PIT_STOP_CHANGE_REAR_TYRES": (
        "Change Rear Tires Only",
        "Pitstop",
        "Commands pit crew to replace only the rear axle tires.",
        "change rears only",
        ["pitstop change rear tires only", "box change rear tires only", "change rear tires"]
    ),
    "PIT_STOP_CHANGE_TYRE_PRESSURE": (
        "Adjust Tire Pressures at Pit Stop",
        "Pitstop",
        "Orders the pit crew to apply preset target pressures to new tire set.",
        "change pit pressures",
        ["pitstop change tire pressures", "box change tire pressures", "adjust tire pressures at stop"]
    ),
    "PIT_STOP_CHANGE_FRONT_PRESSURES": (
        "Adjust Front Tire Pressures at Stop",
        "Pitstop",
        "Adjusts target hot pressures specifically for front tires in the pit menu.",
        "change front pit pressures",
        ["pitstop change front pressures", "box change front pressures"]
    ),
    "PIT_STOP_CHANGE_REAR_PRESSURES": (
        "Adjust Rear Tire Pressures at Stop",
        "Pitstop",
        "Adjusts target hot pressures specifically for rear tires in the pit menu.",
        "change rear pit pressures",
        ["pitstop change rear pressures", "box change rear pressures"]
    ),
    "PIT_STOP_SELECT_TYRE_SET": (
        "Select Specific Tire Set Number",
        "Pitstop",
        "Selects a designated numbered tire set in pit strategy menu (ACC).",
        "select tire set",
        ["pitstop tire set", "box tire set", "select tire set for next stop"]
    ),
    "PIT_STOP_SELECT_LEAST_USED_TYRE_SET": (
        "Select Freshest / Best Tire Set",
        "Pitstop",
        "Automatically identifies and selects the least-worn tire set in allocation (ACC).",
        "select best tires, freshest tires",
        ["pitstop select best tires", "box select best tires", "put on the freshest tire set", "select least used tires"]
    ),
    "PIT_STOP_HARD_TYRES": (
        "Fit Hard Compound Tires",
        "Pitstop",
        "Configures pit menu to mount Hard compound tires at the stop.",
        "box hard tires, pit hard compound",
        ["pitstop hard tires", "box hard tires", "put on the hard tires"]
    ),
    "PIT_STOP_MEDIUM_TYRES": (
        "Fit Medium Compound Tires",
        "Pitstop",
        "Configures pit menu to mount Medium compound tires at the stop.",
        "box medium tires, pit medium compound",
        ["pitstop medium tires", "box medium tires", "put on the medium tires"]
    ),
    "PIT_STOP_SOFT_TYRES": (
        "Fit Soft Compound Tires",
        "Pitstop",
        "Configures pit menu to mount Soft compound tires at the stop.",
        "box soft tires, pit soft compound",
        ["pitstop soft tires", "box soft tires", "put on the soft tires"]
    ),
    "PIT_STOP_SUPERSOFT_TYRES": (
        "Fit Super Soft Compound Tires",
        "Pitstop",
        "Configures pit menu to mount Super Soft compound tires.",
        "box super soft tires",
        ["pitstop super soft tires", "box super soft tires"]
    ),
    "PIT_STOP_ULTRASOFT_TYRES": (
        "Fit Ultra Soft Compound Tires",
        "Pitstop",
        "Configures pit menu to mount Ultra Soft compound tires.",
        "box ultra soft tires",
        ["pitstop ultra soft tires", "box ultra soft tires"]
    ),
    "PIT_STOP_HYPERSOFT_TYRES": (
        "Fit Hyper Soft Compound Tires",
        "Pitstop",
        "Configures pit menu to mount Hyper Soft compound tires.",
        "box hyper soft tires",
        ["pitstop hyper soft tires", "box hyper soft tires"]
    ),
    "PIT_STOP_INTERMEDIATE_TYRES": (
        "Fit Intermediate Rain Tires",
        "Pitstop",
        "Configures pit menu to mount Intermediate grooved wet tires.",
        "box inters, pit intermediates",
        ["pitstop intermediate tires", "box intermediate tires", "put on the inters", "intermediate tires at this stop"]
    ),
    "PIT_STOP_WET_TYRES": (
        "Fit Full Wet Rain Tires",
        "Pitstop",
        "Configures pit menu to mount Full Wet compound tires.",
        "box wets, pit wet tires",
        ["pitstop wet tires", "box wet tires", "put on full wets", "change to wet tires"]
    ),
    "PIT_STOP_DRY_TYRES": (
        "Fit Dry Slick Tires",
        "Pitstop",
        "Configures pit menu to switch back to dry slick tires from wets.",
        "box dry tires, switch to slicks",
        ["pitstop dry tires", "box dry tires", "put on slicks", "switch to dry tires"]
    ),

    # --- PITSTOP - REPAIRS & PENALTIES ---
    "PIT_STOP_FIX_ALL": (
        "Repair All Vehicle Damage at Stop",
        "Pitstop",
        "Orders pit crew to fix all mechanical, suspension, and bodywork damage.",
        "repair all damage, fix everything",
        ["pitstop fix all", "box fix all", "repair everything at this stop", "fix all damage"]
    ),
    "PIT_STOP_FIX_BODY": (
        "Repair Bodywork Damage Only",
        "Pitstop",
        "Orders crew to repair body panels and sheet metal while skipping deep mechanical repairs.",
        "fix bodywork, repair body",
        ["pitstop fix body", "box fix body", "fix bodywork at stop"]
    ),
    "PIT_STOP_FIX_NONE": (
        "Do Not Repair Damage at Stop",
        "Pitstop",
        "Suppresses all repairs to make the pit stop as fast as possible.",
        "no repairs, don't fix damage",
        ["pitstop fix none", "box fix none", "don't repair anything", "skip all repairs"]
    ),
    "PIT_STOP_FIX_ALL_AERO": (
        "Repair All Aero Damage",
        "Pitstop",
        "Instructs pit crew to replace damaged front and rear aerodynamic wings.",
        "fix all aero, repair wings",
        ["pitstop fix all aero", "box fix all aero", "fix all aero damage"]
    ),
    "PIT_STOP_FIX_FRONT_AERO": (
        "Repair Front Splitter / Wing Only",
        "Pitstop",
        "Instructs pit crew to replace only the front aerodynamic components.",
        "fix front aero, replace front wing",
        ["pitstop fix front aero only", "box fix front aero only"]
    ),
    "PIT_STOP_FIX_REAR_AERO": (
        "Repair Rear Wing Only",
        "Pitstop",
        "Instructs pit crew to replace only the rear aerodynamic wing.",
        "fix rear aero, replace rear wing",
        ["pitstop fix rear aero only", "box fix rear aero only"]
    ),
    "PIT_STOP_FIX_NO_AERO": (
        "Do Not Repair Aero Damage",
        "Pitstop",
        "Suppresses aerodynamic wing repairs to save time.",
        "don't fix aero",
        ["pitstop don't fix aero", "box don't fix aero", "leave the aero as is"]
    ),
    "PIT_STOP_FIX_SUSPENSION": (
        "Repair Suspension at Next Stop",
        "Pitstop",
        "Instructs pit crew to align and replace damaged suspension arms.",
        "fix suspension, repair alignment",
        ["pitstop fix suspension", "box fix suspension", "repair suspension at stop"]
    ),
    "PIT_STOP_DONT_FIX_SUSPENSION": (
        "Do Not Repair Suspension",
        "Pitstop",
        "Suppresses suspension repairs during pit stop.",
        "don't fix suspension",
        ["pitstop don't fix suspension", "box don't fix suspension"]
    ),
    "PIT_STOP_SERVE_PENALTY": (
        "Serve Penalty at This Pit Stop",
        "Pitstop",
        "Schedules serving a stop-and-go or time penalty during the pit stop.",
        "serve penalty, take penalty",
        ["pitstop serve penalty", "box serve penalty", "serve my penalty at this stop"]
    ),
    "PIT_STOP_DONT_SERVE_PENALTY": (
        "Do Not Serve Penalty at This Stop",
        "Pitstop",
        "Defers serving penalty until a subsequent lap/stop.",
        "don't serve penalty",
        ["pitstop don't serve penalty", "box don't serve penalty"]
    ),

    # --- PENALTIES & RULES ---
    "DO_I_HAVE_A_PENALTY": (
        "Active Penalty Check",
        "Penalties",
        "Checks whether race control has issued an active penalty against the driver.",
        "penalty check, do I have penalty",
        ["do I have a penalty", "did race control penalize me", "do we have any penalties", "check penalty status"]
    ),
    "DO_I_STILL_HAVE_A_PENALTY": (
        "Pending Penalty Status Check",
        "Penalties",
        "Checks if a previously issued penalty is still pending or unpaid.",
        "penalty still active",
        ["do I still have a penalty", "is my penalty still pending", "do I still need to serve that penalty"]
    ),
    "HAVE_I_SERVED_MY_PENALTY": (
        "Penalty Clearance Confirmation",
        "Penalties",
        "Confirms whether race control has officially cleared driver's served penalty.",
        "penalty served check",
        ["have I served my penalty", "did race control clear my penalty", "is the penalty cleared"]
    ),
    "ENABLE_CUT_TRACK_WARNINGS": (
        "Enable Track Cut Warnings",
        "Penalties",
        "Enables immediate verbal warnings whenever driver exceeds track limits.",
        "warn cuts, track limits on",
        ["enable cut track warnings", "warn about cuts", "play cut track warnings", "track limits warnings on"]
    ),
    "DISABLE_CUT_TRACK_WARNINGS": (
        "Disable Track Cut Warnings",
        "Penalties",
        "Silences verbal warnings for track limit cuts.",
        "no cut warnings, track limits off",
        ["no cut track warnings", "no more cut warnings", "disable cut track warnings", "silence track limit warnings"]
    ),
    "ENABLE_YELLOW_FLAG_MESSAGES": (
        "Enable Yellow Flag Incident Updates",
        "Penalties",
        "Enables automatic audio callouts when local yellow flags or track incidents occur.",
        "yellow flags on, incident updates on",
        ["give me yellows", "tell me yellows", "give me incident updates", "enable yellow flag messages"]
    ),
    "DISABLE_YELLOW_FLAG_MESSAGES": (
        "Disable Yellow Flag Incident Updates",
        "Penalties",
        "Suppresses yellow flag and incident notifications.",
        "yellow flags off, no incident updates",
        ["no more yellows", "stop incident updates", "don't give me yellows", "disable yellow flag messages"]
    ),
    "ENABLE_MANUAL_FORMATION_LAP": (
        "Set Formation Lap Mode (Rolling Start)",
        "Penalties",
        "Informs Crew Chief that the race starts with a manual rolling formation lap.",
        "formation lap, rolling start",
        ["this is the formation lap", "formation lap", "rolling start", "we have a formation lap"]
    ),
    "DISABLE_MANUAL_FORMATION_LAP": (
        "Set Standing Start Mode (No Formation Lap)",
        "Penalties",
        "Informs Crew Chief that the race starts directly from the grid.",
        "standing start, no formation lap",
        ["standing start", "no formation lap", "grid start"]
    ),

    # --- RADIO & SPOTTER CONTROL ---
    "RADIO_CHECK": (
        "Radio Communication Check",
        "Radio",
        "Tests audio link with Chief ('Can you hear me?'). Chief responds to confirm reception.",
        "radio check, mic check, comms check",
        ["radio check", "can you hear me", "comms check", "radio test", "how do you read me"]
    ),
    "REPEAT_LAST_MESSAGE": (
        "Repeat Last Radio Transmission",
        "Radio",
        "Prompts Chief to replay the previous radio message / instruction.",
        "say again, repeat last, repeat message",
        ["repeat last message", "say again", "what was that last message", "repeat that please", "say that again Chief"]
    ),
    "MORE_INFO": (
        "Clarify / Request More Information",
        "Radio",
        "Asks Chief to elaborate on the last statement with further details.",
        "clarify, tell me more, more info",
        ["clarify", "tell me more", "more information", "give me more info", "elaborate on that"]
    ),
    "I_AM_OK": (
        "Confirm Driver OK After Incident",
        "Radio",
        "Responds to Chief's incident welfare query to confirm driver is uninjured and continuing.",
        "I'm ok, I'm fine, driver ok",
        ["I'm OK", "I'm fine", "yes I'm fine", "yeah I'm OK", "all good", "car is moving I'm fine"]
    ),
    "KEEP_QUIET": (
        "Silence Radio / Minimal Chatter",
        "Radio",
        "Silences Crew Chief except for critical safety and flag warnings.",
        "shut up, quiet, silence radio, leave me alone",
        ["keep quiet", "shut up", "I know what I'm doing", "leave me alone", "minimal talking", "quiet on the radio"]
    ),
    "KEEP_ME_INFORMED": (
        "Resume Full Radio Updates",
        "Radio",
        "Re-enables standard ongoing race updates and delta callouts.",
        "keep me updated, resume radio, talk to me",
        ["keep me updated", "keep me informed", "keep me posted", "resume updates", "talk to me Chief"]
    ),
    "TELL_ME_THE_GAPS": (
        "Enable Automatic Lap Delta Callouts",
        "Radio",
        "Instructs Chief to automatically call out time deltas ahead and behind every lap.",
        "tell me gaps, auto deltas on",
        ["tell me the gaps", "tell me the deltas", "give me the deltas", "time deltas every lap", "call out gaps each lap"]
    ),
    "DONT_TELL_ME_THE_GAPS": (
        "Disable Automatic Lap Delta Callouts",
        "Radio",
        "Instructs Chief to stop automatic delta announcements at the start/finish line.",
        "no more gaps, stop deltas",
        ["don't tell me the gaps", "don't tell me the deltas", "no more deltas", "no more gaps", "stop gap callouts"]
    ),
    "DONT_TALK_IN_THE_CORNERS": (
        "Suppress Radio in Braking & Cornering",
        "Radio",
        "Silences radio messages while driver is navigating corners and heavy braking zones.",
        "silence in corners, quiet in braking zones",
        ["don't talk in the corners", "no messages in the corners", "don't talk in the braking zones", "quiet in the turns"]
    ),
    "TALK_TO_ME_ANYWHERE": (
        "Allow Radio Messages Anywhere",
        "Radio",
        "Allows Crew Chief to transmit radio messages at any point on the circuit.",
        "talk anywhere, messages anytime",
        ["talk to me anywhere", "messages at any point", "allow talking in corners"]
    ),
    "SPOT": (
        "Enable Real-Time Spotter",
        "Radio",
        "Turns on the real-time spotter for overlapping car proximity callouts ('car left', 'clear').",
        "enable spotter, spotter on, start spotting",
        ["spot", "start spotting", "enable spotter", "turn spotter on"]
    ),
    "DONT_SPOT": (
        "Disable Real-Time Spotter",
        "Radio",
        "Turns off the spotter proximity callouts.",
        "disable spotter, spotter off, don't spot",
        ["don't spot", "disable spotter", "turn spotter off", "stop spotting"]
    ),
    "STOP_COMPLAINING": (
        "Suppress Chief Complaints",
        "Radio",
        "Tells Chief to stop grumbling or criticizing driving errors.",
        "stop grumbling, stop complaining",
        ["stop complaining", "stop grumbling", "give me a break Chief"]
    ),
    "PLAY_CORNER_NAMES": (
        "Read Track Corner Names",
        "Radio",
        "Reads out turn numbers and official corner names for the current circuit.",
        "corner names, read corners",
        ["read corner names", "tell me the corner names", "what are the corner names"]
    ),

    # --- IN-GAME DISPLAY & MFD NAVIGATION ---
    "DISPLAY_SECTORS": (
        "Display Sector Times MFD Page",
        "Display",
        "Commands in-game MFD to switch to the Sector split times page.",
        "show sectors mfd, display sectors",
        ["display sectors", "display sector times", "MFD sectors", "show sector times"]
    ),
    "DISPLAY_PIT_MENU": (
        "Display Pit Menu MFD Page",
        "Display",
        "Commands in-game MFD to open the pit stop preset and strategy menu.",
        "show pit menu, display pit menu",
        ["display pit menu", "MFD pit menu", "open pit menu", "show pit stop page"]
    ),
    "DISPLAY_TYRES": (
        "Display Tires MFD Page",
        "Display",
        "Commands in-game MFD to open the tire telemetry / wear / pressure screen.",
        "show tires mfd, display tires",
        ["display tires", "MFD tires", "show tire display", "open tire screen"]
    ),
    "DISPLAY_TEMPS": (
        "Display Temperatures MFD Page",
        "Display",
        "Commands in-game MFD to open the temperatures screen.",
        "show temps mfd, display temps",
        ["display temps", "display temperatures", "MFD temps", "show temperature screen"]
    ),
    "DISPLAY_RACE_INFO": (
        "Display Race Info MFD Page",
        "Display",
        "Commands in-game MFD to display session info, laps remaining, and standings.",
        "show race info, display race info",
        ["display race info", "MFD race info", "open race info page"]
    ),
    "DISPLAY_STANDINGS": (
        "Display Standings MFD Page",
        "Display",
        "Commands in-game MFD to show the full race leaderboard standings.",
        "show standings, display standings",
        ["display standings", "MFD standings", "show leaderboard page"]
    ),
    "DISPLAY_PENALTIES": (
        "Display Penalties MFD Page",
        "Display",
        "Commands in-game MFD to display current penalty and incident information.",
        "show penalties mfd",
        ["display penalties", "MFD penalties", "show penalty screen"]
    ),
    "DISPLAY_NEXT": (
        "Cycle to Next MFD Page",
        "Display",
        "Cycles to the next consecutive page in the in-game Multi-Function Display.",
        "next mfd page, mfd next",
        ["display next", "MFD next", "next display page", "cycle MFD"]
    ),

    # --- OVERLAYS & HUD ---
    "SHOW_OVERLAY": (
        "Show HUD Overlay",
        "Overlay",
        "Makes the Crew Chief desktop HUD overlay visible.",
        "show overlay, open overlay",
        ["show overlay", "enable overlay", "display overlay"]
    ),
    "HIDE_OVERLAY": (
        "Hide HUD Overlay",
        "Overlay",
        "Hides the Crew Chief desktop HUD overlay.",
        "hide overlay, close overlay",
        ["hide overlay", "close overlay", "disable overlay"]
    ),
    "SHOW_All_OVERLAYS": (
        "Show All Overlays",
        "Overlay",
        "Enables and displays all configured HUD overlay elements.",
        "show all overlays",
        ["show all overlays", "enable all overlays"]
    ),
    "SHOW_CONSOLE": (
        "Show Diagnostic Console",
        "Overlay",
        "Displays the real-time diagnostic console window.",
        "show console",
        ["show console", "open console"]
    ),
    "HIDE_CONSOLE": (
        "Hide Diagnostic Console",
        "Overlay",
        "Hides the diagnostic console window.",
        "hide console",
        ["hide console", "close console"]
    ),
    "SHOW_CHART": (
        "Show Telemetry Chart Overlay",
        "Overlay",
        "Displays the real-time telemetry trace chart.",
        "show chart, show telemetry chart",
        ["show chart", "display telemetry chart", "open telemetry graph"]
    ),
    "HIDE_CHART": (
        "Hide Telemetry Chart Overlay",
        "Overlay",
        "Hides the telemetry trace chart.",
        "hide chart, close chart",
        ["hide chart", "close chart", "hide telemetry graph"]
    ),
    "CLEAR_CHART": (
        "Reset / Clear Telemetry Chart",
        "Overlay",
        "Clears existing telemetry traces and starts a new comparison graph.",
        "new chart, clear chart",
        ["new chart", "clear chart", "reset telemetry chart"]
    ),
    "SHOW_SUBTITLES": (
        "Show On-Screen Subtitles",
        "Overlay",
        "Enables on-screen text subtitles for voice radio transmissions.",
        "show subtitles, enable subtitles",
        ["show subtitles", "turn subtitles on", "display subtitles"]
    ),
    "HIDE_SUBTITLES": (
        "Hide On-Screen Subtitles",
        "Overlay",
        "Disables on-screen text subtitles.",
        "hide subtitles, disable subtitles",
        ["hide subtitles", "close subtitles", "turn subtitles off"]
    ),
    "TOGGLE_VR_OVERLAYS": (
        "Toggle VR HUD Overlays",
        "Overlay",
        "Toggles visibility of in-headset VR overlay displays.",
        "toggle VR, toggle VR overlay",
        ["toggle VR overlays", "toggle VR HUD", "switch VR overlay"]
    )
}

def generate():
    # Read the raw lines from speech_recognition_config.txt
    raw_voice_lines = {}
    with open('speech_recognition_config.txt', 'r', encoding='utf-8', errors='ignore') as f:
        for line in f:
            line = line.strip()
            if not line or line.startswith('#') or line.startswith('//'):
                continue
            if '=' in line:
                k, v = line.split('=', 1)
                k = k.strip().upper()
                v = v.strip()
                raw_voice_lines[k] = [p.strip() for p in v.split(':') if p.strip()]

    rows = []
    for cmd_id, (name, category, desc, aliases, extra_examples) in CURATED_METADATA.items():
        # Merge raw lines if available, removing contradictions
        examples_set = []
        for ex in extra_examples:
            if ex not in examples_set:
                examples_set.append(ex)

        if cmd_id in raw_voice_lines:
            for raw_ex in raw_voice_lines[cmd_id]:
                # Filter out contradictory or malformed phrases (e.g. bodywork from aero if damage report is better)
                if cmd_id == "HOWS_MY_AERO" and "bodywork" in raw_ex.lower():
                    continue # user noted bodywork belongs in damage report
                if raw_ex.lower() not in [e.lower() for e in examples_set]:
                    examples_set.append(raw_ex)

        examples_str = ";".join(examples_set)
        rows.append({
            "Name": name,
            "CrewChiefCommandId": cmd_id,
            "Category": category,
            "Description": desc,
            "Examples": examples_str,
            "Aliases": aliases,
            "IsEnabled": "True"
        })

    # Sort rows by Category and Name
    rows.sort(key=lambda r: (r["Category"], r["Name"]))

    # Output CSV file
    out_csv = "CrewChief_All_Commands.csv"
    fieldnames = ["Name", "CrewChiefCommandId", "Category", "Description", "Examples", "Aliases", "IsEnabled"]
    with open(out_csv, "w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames, quoting=csv.QUOTE_ALL)
        writer.writeheader()
        for r in rows:
            writer.writerow(r)

    print(f"Generated {len(rows)} curated commands in '{out_csv}'.")
    return rows

if __name__ == '__main__':
    generate()

