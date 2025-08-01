namespace MolenApplicatie.Server.Utils
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class NotPatchable : Attribute
    {
        public NotPatchable() { }
    }
}
