import { Place } from "../Interfaces/Place";

export class PlaceClass implements Place {
  id: number;
  name: string;
  lat: number;
  lon: number;
  population: number;

  constructor(id: number, name: string, lat: number, lon: number, population: number) {
    this.id = id;
    this.name = name;
    this.lat = lat;
    this.lon = lon;
    this.population = population;
  }
}
