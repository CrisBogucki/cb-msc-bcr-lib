using System;

namespace CBMscBrcLib.Attribute
{
    [System.AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class Service : System.Attribute
    {
        public readonly string RoutingKey;

        public readonly byte VerMajor;

        public readonly byte VerMinor;

        public readonly string Description;

        public Service(string routingKey, string description, byte verMajor = 1, byte verMinor = 0 )
        {
            this.RoutingKey = routingKey;
            this.Description = description;
            this.VerMajor = verMajor;
            this.VerMinor = verMinor;
        }
    }
}
