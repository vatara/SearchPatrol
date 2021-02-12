using System;

namespace SearchPatrol.Common.SimObjects
{
    public abstract class SimObject
    {
        protected Random random = new Random();

        public abstract string Random();
    }
}
