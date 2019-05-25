using Rage;
using System.Windows.Forms;

namespace MotorCade
{
    public static class AirGuard
    {
        public static bool isChopperFollowing = false;
        public static void setUpPoliceHelicopter()
        {
            Vector3 helicopterSpawnPos = Game.LocalPlayer.Character.GetOffsetPositionFront(30f);
            Vehicle policeHelicopter = new Vehicle("POLMAV", helicopterSpawnPos, 310f);
            Ped pilot = new Ped("cs_fbisuit_01", helicopterSpawnPos.Around2D(5f), 310f);
            pilot.Tasks.EnterVehicle(policeHelicopter, -1);
            followTheLeadCar(policeHelicopter);
        }

        public static void followTheLeadCar(Vehicle helicopter)
        {
            isChopperFollowing = true;
            GameFiber.StartNew(delegate
            {
                try
                {

                    Game.LogTrivial("Chopper starting...");
                    Ped playerPed = Game.LocalPlayer.Character;

                    Vehicle leader = playerPed.CurrentVehicle;
                    if (!playerPed.IsInAnyVehicle(false))
                    {
                        isChopperFollowing = false;
                        return;
                    }

                    Ped followingDriver = helicopter.Driver;

                    Blip blip = followingDriver.AttachBlip();
                    blip.Flash(500, -1);
                    blip.Color = System.Drawing.Color.Aqua;
                    Vector3 flyPos = leader.GetOffsetPositionUp(20f);

                    followingDriver.Tasks.DriveToPosition(leader.GetOffsetPositionUp(20f), 9f,
                                            VehicleDrivingFlags.FollowTraffic | VehicleDrivingFlags.YieldToCrossingPedestrians);
                    float speed = 13f;
                    while (true)
                    {
                        followingDriver.Tasks.DriveToPosition(leader.GetOffsetPositionUp(20f), speed, VehicleDrivingFlags.IgnorePathFinding);
                        GameFiber.Sleep(60);

                        if (!isChopperFollowing)
                        {
                            break;
                        }
                        // Break if the player gets fown from the vehicle
                        if (!playerPed.IsInVehicle(leader, false))
                        {
                            break;
                        }
                        speed = leader.Speed;
                        if (Vector3.Distance(leader.Position, helicopter.Position) > 500f)
                        {
                            helicopter.Position = leader.GetOffsetPosition(Vector3.RelativeBack * 20f);
                            helicopter.Heading = leader.Heading;
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
                            if (Vector3.Distance(leader.Position, helicopter.Position) > 21f)
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
                    helicopter.Driver.Delete();
                    helicopter.Delete();
                    isChopperFollowing = false;
                }
            });
        }
    }
}
