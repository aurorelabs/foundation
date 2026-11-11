using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;

/// <summary>Column shortcuts for properties.</summary>
public static class PropertyBuilderExtensions
{
    /// <summary>Column configuration for a property.</summary>
    /// <typeparam name="TProperty">The CLR type of the property.</typeparam>
    extension<TProperty>(PropertyBuilder<TProperty> propertyBuilder)
    {
        /// <summary>Stores the property as <c>timestamptz</c> (with time zone) or <c>timestamp</c> (without).</summary>
        /// <param name="hasTimezone">True for <c>timestamptz</c>, the default; false for <c>timestamp</c>.</param>
        public PropertyBuilder<TProperty> AsTimestampColumn(bool hasTimezone = true)
        {
            return propertyBuilder.HasColumnType(hasTimezone ? "timestamptz" : "timestamp");
        }

        /// <summary>Sets the precision and scale of a decimal column.</summary>
        /// <param name="precision">The total number of digits. Defaults to 10.</param>
        /// <param name="scale">The digits after the decimal point. Defaults to 2.</param>
        public PropertyBuilder<TProperty> AsDecimalColumn(int precision = 10, int scale = 2)
        {
            return propertyBuilder.HasPrecision(precision, scale);
        }
    }
}
