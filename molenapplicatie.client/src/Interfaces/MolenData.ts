import { MolenType } from "../Class/MolenType";

export interface MolenData {
  id: number;
  name: string;
  bouwjaar?: number;
  herbouwdJaar?: string;
  bouwjaarStart?: number;
  bouwjaarEinde?: number;
  functie: string;
  tenBruggeNr: string;
  plaats: string;
  adres: string;
  modelType: MolenType[];
  north: number;
  east: number;
  lastUpdated: Date;

  getBouwjaar(): string;
}
