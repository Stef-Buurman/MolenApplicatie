import { MarkerInfo } from "../Interfaces/Map/MarkerInfo";

export class MapInformation {
  MapId: string;
  Map!: L.Map;
  Markers: MarkerInfo[] = [];

  constructor(mapId: string, map: L.Map, markers: MarkerInfo[] = []) {
    this.MapId = mapId;
    this.Map = map;
    this.Markers = markers;
  }
}
