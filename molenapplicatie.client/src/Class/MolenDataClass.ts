import { MolenData } from "../Interfaces/MolenData";
import { MolenType } from "./MolenType";

export class MolenDataClass implements MolenData {
  id: number;
  name: string;
  bouwjaar?: number;
  herbouwdJaar?: string;
  bouwjaarStart?: number;
  bouwjaarEinde?: number;
  functie: string;
  ten_Brugge_Nr: string;
  plaats: string;
  adres: string;
  modelType: MolenType[] = [];
  north: number;
  east: number;
  lastUpdated: Date;

  constructor(
    id: number,
    name: string,
    functie: string,
    ten_Brugge_Nr: string,
    plaats: string,
    adres: string,
    north: number,
    east: number,
    modelType: MolenType[] = [],
    lastUpdated: Date,
    bouwjaar?: number,
    herbouwdJaar?: string,
    bouwjaarStart?: number,
    bouwjaarEinde?: number
  ) {
    this.id = id;
    this.name = name;
    this.bouwjaar = bouwjaar;
    this.herbouwdJaar = herbouwdJaar;
    this.bouwjaarStart = bouwjaarStart;
    this.bouwjaarEinde = bouwjaarEinde;
    this.functie = functie;
    this.ten_Brugge_Nr = ten_Brugge_Nr;
    this.plaats = plaats;
    this.adres = adres;
    this.modelType = modelType;
    this.north = north;
    this.east = east;
    this.lastUpdated = lastUpdated;
  }

  // Method matches the interface signature
  getBouwjaar(): string {
    if (this.bouwjaar !== undefined) {
      return this.bouwjaar.toString();
    } else if (this.bouwjaarStart !== undefined && this.bouwjaarEinde !== undefined) {
      return `${this.bouwjaarStart} - ${this.bouwjaarEinde}`;
    } else if (this.bouwjaarStart !== undefined) {
      return this.bouwjaarStart.toString();
    } else if (this.bouwjaarEinde !== undefined) {
      return this.bouwjaarEinde.toString();
    } else {
      return "Onbekend";
    }
  }
}
