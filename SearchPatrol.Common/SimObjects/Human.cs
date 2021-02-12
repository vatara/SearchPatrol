namespace SearchPatrol.Common.SimObjects
{
    public class Human : SimObject
    {
        enum Type { Marshaller, Tarmac }
        enum Gender { Male, Female }
        enum Season { Summer, Winter }
        enum Ethnicity { African, Arab, Asian, Caucasian, Hispanic, Indian }

        public override string Random()
        {
            var humanType = random.Next(0, 2);
            var gender = random.Next(0, 2);
            var season = random.Next(0, 2);
            var ethnicity = random.Next((int)Ethnicity.African, (int)Ethnicity.Indian);
            return $"{(Type)humanType}_{(Gender)gender}_{(Season)season}_{(Ethnicity)ethnicity}";
        }
    }
}
