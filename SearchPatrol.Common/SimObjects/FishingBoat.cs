namespace SearchPatrol.Common.SimObjects
{
    public class FishingBoat : SimObject
    {
        enum Type { FishingBoat, FishingShip02, FishingShip03 }

        public override string Random()
        {
            return $"{(Type)random.Next(0, (int)Type.FishingShip03 + 1)}";
        }
    }
}
