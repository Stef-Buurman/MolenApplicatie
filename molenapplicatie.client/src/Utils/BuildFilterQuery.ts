import { FilterFormValues } from "../Interfaces/Filters/Filter";

export function BuildFilterQuery(filters: FilterFormValues[]): string {
  const params = new URLSearchParams();

  filters.forEach((filter: FilterFormValues) => {
    if (
      filter.filterNameMax &&
      filter.maxValue !== undefined &&
      filter.maxValue !== null
    ) {
      const stringValue =
        filter.maxValue instanceof Date
          ? filter.maxValue.toISOString()
          : String(filter.maxValue);

      params.append(filter.filterNameMax, stringValue);
    }
    if (
      filter.filterName &&
      filter.value !== undefined &&
      filter.value !== null
    ) {
      const stringValue =
        filter.value instanceof Date
          ? filter.value.toISOString()
          : String(filter.value);

      params.append(filter.filterName, stringValue);
    }
  });
  return `?${params.toString()}`;
}
