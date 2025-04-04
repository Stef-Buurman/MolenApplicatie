import { MolenImage } from "../Class/MolenImage";
import { MolenType } from "../Class/MolenType";
import { MolenMaker } from "../Class/MolenMaker";

export interface MolenData {
  id: number; 
  name: string; 
  ten_Brugge_Nr: string; 
  toelichtingNaam?: string; 
  bouwjaar?: number;  //2
  herbouwdJaar?: string;  //2
  bouwjaarStart?: number;  //2
  bouwjaarEinde?: number;  //2
  functie: string; 
  doel?: string; 
  toestand?: string; 
  bedrijfsvaardigheid?: string; 
  plaats: string;

  adres: string; 
  provincie?: string; 
  gemeente?: string; 
  streek?: string; 
  plaatsaanduiding?: string; 
  opvolger?: string; 
  voorganger?: string; 
  verplaatstNaar?: string; 
  afkomstigVan?: string; 
  literatuur?: string; 
  plaatsenVoorheen?: string; 
  wiekvorm?: string; 
  wiekVerbeteringen?: string; 
  monument?: string; 
  plaatsBediening?: string; 
  bedieningKruiwerk?: string; 
  plaatsKruiwerk?: string; 
  kruiwerk?: string; 
  vlucht?: string; 
  openingstijden?: string; 
  openVoorPubliek: boolean; 
  openOpZaterdag: boolean; 
  openOpZondag: boolean; 
  openOpAfspraak: boolean; 
  krachtbron?: string; 
  website?: string; 
  winkelInformatie?: string; 
  bouwbestek?: string; 
  bijzonderheden?: string; 
  museuminformatie?: string; 
  molenaar?: string; 
  molenMakers?: MolenMaker[]; 
  eigendomshistorie?: string; 
  molenerf?: string; 
  trivia?: string; 
  geschiedenis?: string; 
  wetenswaardigheden?: string; 
  wederopbouw?: string; 
  as?: string; 
  wieken?: string; 
  toegangsprijzen?: string; 
  uniekeEigenschap?: string; 
  landschappelijkeWaarde?: string; 
  kadastraleAanduiding?: string; 
  canAddImages: boolean; 
  eigenaar?: string; 
  recenteWerkzaamheden?: string; 
  rad?: string; 
  radDiameter?: string; 
  wateras?: string; 
  lat: number; 
  long: number; 
  lastUpdated?: Date; 
  images?: MolenImage[]; 
  addedImages?: MolenImage[]; 
  modelType: MolenType[]; 
  hasImage: boolean; 
}
