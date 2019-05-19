using Rage;
using System.Drawing;

[assembly: Rage.Attributes.Plugin("Motorcade", Description = "Spawns a motorcade and makes them follow the player.")]

public static class EntryPoint
{
    public static bool isMotorcadeInProgress = false;
    public static void Main()
    {
        Ped[] drivers = new Ped[6];
        Vehicle[] motorcadeVehicles = new Vehicle[6];
        //Vehicle suv1, suv2, sedan1, sedan2, stretch, riot = null;

        float[] vehicleSpanPositionConsts = { -20f, -35f, -50f, -70f, -85f, -100f };
        string[] vehicleModels = { "FBI2", "FBI2", "STRETCH", "FBI", "FBI", "RIOT" };

        // Get a position 15 meters in front of the player.
        Vector3[] vehicleSpanPositions = new Vector3[6];// Game.LocalPlayer.Character.GetOffsetPositionFront(20f);
        Vector3[] driverSpanPositions = new Vector3[6];

        // Set driver and vehicle spawnning positions
        for (int i = 0; i < 6; i++)
        {
            vehicleSpanPositions[i] = Game.LocalPlayer.Character.GetOffsetPositionFront(vehicleSpanPositionConsts[i]);
            driverSpanPositions[i] = Game.LocalPlayer.Character.GetOffsetPositionFront(vehicleSpanPositionConsts[i] - 5f);
        }

        // Spawn driver peds
        for (int i = 0; i < 6; i++)
        {
            drivers[i] = new Ped("cs_fbisuit_01", driverSpanPositions[i], 130f);
            // Prevent the character from doing something we haven't told it to do (Eg. fleeing from gunfire).
            drivers[i].BlockPermanentEvents = true;
        }

        // Yield for 3 real time seconds.
        GameFiber.Sleep(3000);

        // Spawn Vehicles
        for (int i = 0; i < 6; i++)
        {
            motorcadeVehicles[i] = new Vehicle(vehicleModels[i], vehicleSpanPositions[i], 130f);
        }

        // Wait for 3 seconds   
        GameFiber.Wait(3000);

        // Since we've yielded, letting the game and other plugins run, our character and vehicle may have been deleted in the mean time.
        // The character may also have died, or the vehicle may have blown up.
        // Let's verify that's not the case.
        foreach (Vehicle vehicle in motorcadeVehicles)
        {
            if (!vehicle.Exists())
            {
                // Inform the user.
                Game.DisplayNotification("The vehicle was killed or deleted prematurely.");
                return;
            }
        }

        foreach (Ped ped in drivers)
        {
            if (ped.IsDead)
            {
                // Inform the user.
                Game.DisplayNotification("One of the drivers was killed or deleted prematurely.");
                return;
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
                && drivers[4].IsInVehicle(motorcadeVehicles[4], false))
            {
                // Stop waiting.
                break;
            }

            GameFiber.Yield();
        }

        // Makes the first vehilce tail the player
        followTheVehice(motorcadeVehicles[0], Game.LocalPlayer.Character.CurrentVehicle);

        // Do the same for all remaining vehicles. All vehicles tail their predecessor.
        for (int i = 1; i < 6; i++)
        {
            followTheVehice(motorcadeVehicles[i - 1], motorcadeVehicles[i]);
        }
    }

    public static void followTheVehice(Vehicle following, Vehicle followed)
    {
        isMotorcadeInProgress = true;
        GameFiber.StartNew(delegate
        {
            try
            {
                following.ShouldVehiclesYieldToThisVehicle = true;
                following.IsSirenOn = true;
                followed.ShouldVehiclesYieldToThisVehicle = true;
                followed.IsSirenOn = true;

                Game.LogTrivial("Following");
                Ped playerPed = Game.LocalPlayer.Character;
                if (!playerPed.IsInAnyVehicle(false))
                {
                    isMotorcadeInProgress = false;
                    return;
                }

                if (following == null)
                {
                    Game.DisplayNotification("The vehicle following was lost!");
                    isMotorcadeInProgress = false;
                    return;
                }

                Ped followingDriver = following.Driver;

                Blip blip = followingDriver.AttachBlip();
                blip.Flash(500, -1);
                blip.Color = System.Drawing.Color.Aqua;
                followingDriver.Tasks.DriveToPosition(followed.GetOffsetPosition(Vector3.RelativeBack * 3f), 9f, VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);
                GameFiber.Sleep(100);
                float speed = 13f;
                while (true)
                {

                    followingDriver.Tasks.DriveToPosition(followed.GetOffsetPosition(Vector3.RelativeBack * 3f), speed, VehicleDrivingFlags.IgnorePathFinding);
                    GameFiber.Sleep(60);

                    if (!isMotorcadeInProgress)
                    {
                        break;
                    }
                    // Break if any one of the drivers gets down from their vehicle
                    if (!playerPed.IsInVehicle(followed, false))
                    {
                        break;
                    }
                    speed = followed.Speed;
                    if (Vector3.Distance(followed.Position, following.Position) > 150f)
                    {
                        following.Position = followed.GetOffsetPosition(Vector3.RelativeBack * 7f);
                        following.Heading = followed.Heading;
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
                        if (Vector3.Distance(followed.Position, following.Position) > 21f)
                        {
                            speed = 17f;
                        }
                    }

                }
                if (blip.Exists()) { blip.Delete(); }
            }
            catch
            {
            }
            finally
            {
                isMotorcadeInProgress = false;
            }
        });
    }
}