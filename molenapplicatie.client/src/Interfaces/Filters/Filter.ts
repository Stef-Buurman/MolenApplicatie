export type FilterType = 'number' | 'date' | 'string' | 'timespan' | 'boolean';

export interface FilterOption {
  name: string;
  filterName: string;
  filterNameMax?: string;
  type: FilterType;
  values?: (number | string | Date)[];
}

export interface FilterFormValues {
  name: string;
  filterName: string;
  filterNameMax?: string;
  type: FilterType;
  value: number | string | Date | boolean | null;
  maxValue?: number | string | Date | null;
  isAList: boolean;
}
