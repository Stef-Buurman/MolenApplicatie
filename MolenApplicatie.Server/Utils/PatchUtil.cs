using System.Reflection;

namespace MolenApplicatie.Server.Utils
{
    public static class PatchUtil
    {
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
    }
}
