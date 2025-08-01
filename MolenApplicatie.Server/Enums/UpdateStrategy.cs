namespace MolenApplicatie.Server.Enums
{
    public enum UpdateStrategy
    {
        /// <summary>
        /// Updates all values of the entity 
        /// </summary>
        Put,
        /// <summary>
        /// Updates all values of the entity that are not the default value
        /// </summary>
        Patch,
        /// <summary>
        /// replaces the entity with the already existing entity
        /// </summary>
        Ignore
    }
}
