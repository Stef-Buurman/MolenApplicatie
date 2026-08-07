import { MolenData, MolenType, Place } from '../api/generated/data-contracts';

export interface SearchModel<T> {
  reference: string;
  data: T;
}

export interface SearchModelWithCount<T> extends SearchModel<T> {
  count: number;
}

export interface KeyValuePair<K, V> {
  key: K;
  value: V;
}

export interface SearchResultsModel {
  molens: SearchModel<MolenData>[];
  places: Array<KeyValuePair<string, SearchModel<Place>[]>>;
  molenTypes: SearchModelWithCount<MolenType>[];
}
