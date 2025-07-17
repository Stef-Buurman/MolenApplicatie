using System.Reflection;

namespace MolenApplicatie.Server.Utils
{
    public static class PatchUtil
    {
        /// <summary>
        /// replaces all default values of the new entity with the values of the old entity
        /// </summary>
        /// <typeparam name="TEntity">the entity to patch</typeparam>
        /// <param name="newEntity">the new entity with the empty/default values that needs to be patched</param>
        /// <param name="oldEntity">the old entity without empty/default values that have to be transfered to the new entity</param>
        public static void Patch<TEntity>(TEntity newEntity, TEntity oldEntity) where TEntity : class
        {
            Type entityType = typeof(TEntity);
            foreach (PropertyInfo property in entityType.GetProperties())
            {
                if (!property.CanWrite
                    || !property.CanRead)
                    continue;

                if (property.GetSetMethod() == null
                    || !property.GetSetMethod()!.IsPublic)
                    continue;

                if (property.GetCustomAttribute(typeof(NotPatchable)) != null)
                    continue;

                object? newValue = property.GetValue(newEntity);
                if (newValue == null)
                {
                    var oldValue = property.GetValue(oldEntity);
                    property.SetValue(newEntity, oldValue, null);
                }
                else if (
                    property.PropertyType.IsValueType
                    && newValue.Equals(Activator.CreateInstance(property.PropertyType)))
                {
                    var oldValue = property.GetValue(oldEntity);
                    property.SetValue(newEntity, oldValue, null);
                }
            }
        }
        /// <summary>
        /// replaces all default values of the new entity with the values of the old entity
        /// </summary>
        /// <typeparam name="TEntity">the entity to patch</typeparam>
        /// <param name="newEntity">the new entity with the empty/default values that needs to be patched</param>
        /// <param name="oldEntity">the old entity without empty/default values that have to be transfered to the new entity</param>
        public static void PatchWith<TEntity>(this TEntity newEntity, TEntity oldEntity) where TEntity : class
            => Patch<TEntity>(newEntity, oldEntity);
    }
}
