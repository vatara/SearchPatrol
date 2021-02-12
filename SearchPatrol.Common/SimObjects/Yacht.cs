namespace SearchPatrol.Common.SimObjects
{
    public class Yacht : SimObject
    {
        enum Type { Yacht01, Yacht02, Yacht03 }

        public override string Random()
        {
            return $"{(Type)random.Next(0, 2)}";
        }
    }
}
