using Rage;
using System.Windows.Forms;

[assembly: Rage.Attributes.Plugin("Motorcade", Description = "Spawns a motorcade and makes them follow the player.")]

namespace MotorCade
{
    public static class EntryPoint
    {
        public static bool isMotorcadeInProgress = false;
        public static void Main()
        {
            Ped[] drivers = new Ped[6];
            Vehicle[] motorcadeVehicles = new Vehicle[6];

            bool allSetUp = setUpPedsAndVehicles(drivers, motorcadeVehicles);

            //Set up the police chopper
            //AirGuard.setUpPoliceHelicopter();

            if (allSetUp)
            {
                // All vehicles tail their predecessor.
                for (int i = 0; i < 6; i++)
                {
                    followTheLeader(motorcadeVehicles[i], 2f + i * 12f);
                }
            }
            else
            {
                Game.DisplayNotification("Setting up vehicles and peds failed.");
            }
            //while (true)
            //{
            //    if (Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(PropertiesInitializer.motorcadeModifierKey) &&
            //        Albo1125.Common.CommonLibrary.ExtensionMethods.IsKeyDownRightNowComputerCheck(PropertiesInitializer.motorcadeEndKey))
            //    {
            //        doCleanUp(motorcadeVehicles, drivers);
            //        Game.DisplayNotification("Motorcade is cleaned up!");
            //    }
            //}
        }

        private static void doCleanUp(Vehicle[] motorcadeVehicles, Ped[] drivers)
        {
            for (int i = 0; i < 6; i++)
            {
                motorcadeVehicles[i].Delete();
                drivers[i].Delete();
                motorcadeVehicles[i] = null;
                drivers[i] = null;
            }
        }

        public static bool setUpPedsAndVehicles(Ped[] drivers, Vehicle[] motorcadeVehicles)
        {
            float[] vehicleSpanPositionConsts = { -20f, -35f, -50f, -70f, -85f, -100f };
            string[] vehicleModels = { "WASHINGTON", "WASHINGTON", "FBI2", "EMPEROR", "FBI2", "FBI2" };

            // Get a position 15 meters in front of the player.
            Vector3[] vehicleSpanPositions = new Vector3[6];
            Vector3[] driverSpanPositions = new Vector3[6];

            // Set driver and vehicle spawnning positions
            for (int i = 0; i < 6; i++)
            {
                vehicleSpanPositions[i] = Game.LocalPlayer.Character.GetOffsetPositionFront(vehicleSpanPositionConsts[i]);
                driverSpanPositions[i] = Game.LocalPlayer.Character.GetOffsetPositionFront(vehicleSpanPositionConsts[i] + 3f);
            }

            // Spawn driver peds
            for (int i = 0; i < 6; i++)
            {
                drivers[i] = new Ped("cs_fbisuit_01", driverSpanPositions[i], 310f);
                // Prevent the character from doing something we haven't told it to do (Eg. fleeing from gunfire).
                drivers[i].BlockPermanentEvents = true;
            }

            // Yield for 3 real time seconds.
            GameFiber.Sleep(3000);

            // Spawn Vehicles
            for (int i = 0; i < 6; i++)
            {
                motorcadeVehicles[i] = new Vehicle(vehicleModels[i], vehicleSpanPositions[i], 310f);
            }

            // Wait for 3 seconds   
            GameFiber.Wait(3000);

            // Since we've yielded, letting the game and other plugins run, our character and vehicle may have been deleted in the mean time.
            // The character may also have died, or the vehicle may have blown up. Let's verify that's not the case.
            foreach (Vehicle vehicle in motorcadeVehicles)
            {
                if (!vehicle.Exists())
                {
                    // Inform the user.
                    Game.DisplayNotification("One of the vehicles was killed or deleted prematurely.");
                    return false;
                }
            }

            foreach (Ped ped in drivers)
            {
                if (ped.IsDead)
                {
                    // Inform the user.
                    Game.DisplayNotification("One of the drivers was killed or deleted prematurely.");
                    return false;
                }
            }

            Task[] tasks = new Task[6];
            // Make the ped enters the vehicle on the driver's seat (Second parameter is the passenger seat index, thus -1 is the driver's seat).
            for (int i = 0; i < 6; i++)
            {
                tasks[i] = drivers[i].Tasks.EnterVehicle(motorcadeVehicles[i], -1);
            }

            // Yield the fiber until the peds have gotten into their vehicle.
            while (true)
            {
                // Are all characters in the vehicle?
                if (drivers[0].IsInVehicle(motorcadeVehicles[0], false) && drivers[1].IsInVehicle(motorcadeVehicles[1], false)
                    && drivers[2].IsInVehicle(motorcadeVehicles[2], false) && drivers[3].IsInVehicle(motorcadeVehicles[3], false)
                    && drivers[4].IsInVehicle(motorcadeVehicles[4], false) && drivers[5].IsInVehicle(motorcadeVehicles[5], false))
                {
                    // Stop waiting. Call execution success.
                    return true;
                }

                GameFiber.Yield();
            }
        }

        public static void followTheLeader(Vehicle following, float distance)
        {
            isMotorcadeInProgress = true;
            GameFiber.StartNew(delegate
            {
                try
                {
                    following.ShouldVehiclesYieldToThisVehicle = true;
                    following.IsSirenOn = true;
                    //followed.ShouldVehiclesYieldToThisVehicle = true;
                    //followed.IsSirenOn = true;

                    Game.LogTrivial("Motorcade starting...");
                    Ped playerPed = Game.LocalPlayer.Character;

                    Vehicle leader = playerPed.CurrentVehicle;
                    if (!playerPed.IsInAnyVehicle(false))
                    {
                        isMotorcadeInProgress = false;
                        return;
                    }

                    Ped followingDriver = following.Driver;

                    Blip blip = followingDriver.AttachBlip();
                    blip.Flash(500, -1);
                    blip.Color = System.Drawing.Color.Aqua;
                    followingDriver.Tasks.DriveToPosition(leader.GetOffsetPosition(Vector3.RelativeBack * distance), 9f, 
                        VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);
                    float speed = 13f;
                    while (true)
                    {

                        followingDriver.Tasks.DriveToPosition(leader.GetOffsetPosition(Vector3.RelativeBack * distance), speed, 
                            VehicleDrivingFlags.DriveAroundVehicles | VehicleDrivingFlags.AllowWrongWay | VehicleDrivingFlags.Emergency | VehicleDrivingFlags.IgnorePathFinding);
                        following.Heading = leader.Heading;
                        //GameFiber.Sleep(60);

                        if (!isMotorcadeInProgress)
                        {
                            break;
                        }
                        // Break if the player gets fown from the vehicle
                        if (!playerPed.IsInVehicle(leader, false))
                        {
                            break;
                        }
                        speed = leader.Speed;
                        if (Vector3.Distance(leader.Position, following.Position) > 200f)
                        {
                            following.Position = leader.GetOffsetPosition(Vector3.RelativeBack * distance);
                            following.Heading = leader.Heading;
                            blip.Delete();
                            blip = followingDriver.AttachBlip();
                            blip.Flash(500, -1);
                            blip.Color = System.Drawing.Color.Aqua;
                        }
                        else
                        {
                            if (speed > 20f) { speed = 20f; }
                            else if (speed < 10f)
                            {
                                speed = 10f;
                            }
                            if (Vector3.Distance(leader.Position, following.Position) > 21f)
                            {
                                speed = 17f;
                            }
                        }
                    }
                    if (blip.Exists()) { blip.Delete(); }
                }
                catch (System.Exception ex)
                {
                    Game.LogTrivial("An exception occured " + ex.Data);
                }
                finally
                {
                    following.Driver.Delete();
                    following.Delete();
                    isMotorcadeInProgress = false;
                }
            });
        }

        internal class PropertiesInitializer
        {
            private static KeysConverter kc = new KeysConverter();
            public static Keys motorcadeEndKey { get; set; }
            public static Keys motorcadeModifierKey { get; set; }
            private static void loadValuesFromIniFile()
            {

                try
                {
                    motorcadeModifierKey = (Keys)kc.ConvertFromString(getMotorcadeModifierKey());
                    motorcadeEndKey = (Keys)kc.ConvertFromString(getMotorcadeEndKey());
                }
                catch
                {
                    motorcadeModifierKey = Keys.LControlKey;
                    motorcadeEndKey = Keys.D0;
                }
            }

            private static string getMotorcadeModifierKey()
            {
                InitializationFile ini = initialiseFile();
                string key = ini.ReadString("Keybindings", "MotorcadeModifierKey", "LControlKey");
                return key;
            }

            private static string getMotorcadeEndKey()
            {
                InitializationFile ini = initialiseFile();
                string key = ini.ReadString("Keybindings", "MotorcadeEndKey", "D0");
                return key;
            }

            public static InitializationFile initialiseFile()
            {
                InitializationFile ini = new InitializationFile("plugins/LSPDFR/Motorcade.ini");
                ini.Create();
                return ini;
            }
        }
    }
}