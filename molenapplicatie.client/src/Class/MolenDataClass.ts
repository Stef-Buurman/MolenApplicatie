import { MolenData } from "../Interfaces/MolenData";
import { MolenImage } from "./MolenImage";
import { MolenType } from "./MolenType";

export class MolenDataClass implements MolenData {
  id: number;
  name: string;
  ten_Brugge_Nr: string;
  toelichtingNaam?: string;
  bouwjaar?: number;
  herbouwdJaar?: string;
  bouwjaarStart?: number;
  bouwjaarEinde?: number;
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

  constructor(
    id: number,
    name: string,
    ten_Brugge_Nr: string,
    functie: string,
    plaats: string,
    adres: string,
    openVoorPubliek: boolean,
    openOpZaterdag: boolean,
    openOpZondag: boolean,
    openOpAfspraak: boolean,
    lat: number,
    long: number,
    hasImage: boolean,
    modelType: MolenType[] = [],
    canAddImages: boolean,
    // Additional optional properties:
    toelichtingNaam?: string,
    bouwjaar?: number,
    herbouwdJaar?: string,
    bouwjaarStart?: number,
    bouwjaarEinde?: number,
    doel?: string,
    toestand?: string,
    bedrijfsvaardigheid?: string,
    provincie?: string,
    gemeente?: string,
    streek?: string,
    plaatsaanduiding?: string,
    opvolger?: string,
    voorganger?: string,
    verplaatstNaar?: string,
    afkomstigVan?: string,
    literatuur?: string,
    plaatsenVoorheen?: string,
    wiekvorm?: string,
    wiekVerbeteringen?: string,
    monument?: string,
    plaatsBediening?: string,
    bedieningKruiwerk?: string,
    plaatsKruiwerk?: string,
    kruiwerk?: string,
    vlucht?: string,
    openingstijden?: string,
    krachtbron?: string,
    website?: string,
    winkelInformatie?: string,
    bouwbestek?: string,
    bijzonderheden?: string,
    museuminformatie?: string,
    molenaar?: string,
    eigendomshistorie?: string,
    molenerf?: string,
    trivia?: string,
    geschiedenis?: string,
    wetenswaardigheden?: string,
    wederopbouw?: string,
    as?: string,
    wieken?: string,
    toegangsprijzen?: string,
    uniekeEigenschap?: string,
    landschappelijkeWaarde?: string,
    kadastraleAanduiding?: string,
    eigenaar?: string,
    recenteWerkzaamheden?: string,
    rad?: string,
    radDiameter?: string,
    wateras?: string,
    addedImages?: MolenImage[],
    images?: MolenImage[],
    lastUpdated?: Date
  ) {
    this.id = id;
    this.name = name;
    this.ten_Brugge_Nr = ten_Brugge_Nr;
    this.toelichtingNaam = toelichtingNaam;
    this.bouwjaar = bouwjaar;
    this.herbouwdJaar = herbouwdJaar;
    this.bouwjaarStart = bouwjaarStart;
    this.bouwjaarEinde = bouwjaarEinde;
    this.functie = functie;
    this.doel = doel;
    this.toestand = toestand;
    this.bedrijfsvaardigheid = bedrijfsvaardigheid;
    this.plaats = plaats;
    this.adres = adres;
    this.provincie = provincie;
    this.gemeente = gemeente;
    this.streek = streek;
    this.plaatsaanduiding = plaatsaanduiding;
    this.opvolger = opvolger;
    this.voorganger = voorganger;
    this.verplaatstNaar = verplaatstNaar;
    this.afkomstigVan = afkomstigVan;
    this.literatuur = literatuur;
    this.plaatsenVoorheen = plaatsenVoorheen;
    this.wiekvorm = wiekvorm;
    this.wiekVerbeteringen = wiekVerbeteringen;
    this.monument = monument;
    this.plaatsBediening = plaatsBediening;
    this.bedieningKruiwerk = bedieningKruiwerk;
    this.plaatsKruiwerk = plaatsKruiwerk;
    this.kruiwerk = kruiwerk;
    this.vlucht = vlucht;
    this.openingstijden = openingstijden;
    this.openVoorPubliek = openVoorPubliek;
    this.openOpZaterdag = openOpZaterdag;
    this.openOpZondag = openOpZondag;
    this.openOpAfspraak = openOpAfspraak;
    this.krachtbron = krachtbron;
    this.website = website;
    this.winkelInformatie = winkelInformatie;
    this.bouwbestek = bouwbestek;
    this.bijzonderheden = bijzonderheden;
    this.museuminformatie = museuminformatie;
    this.molenaar = molenaar;
    this.eigendomshistorie = eigendomshistorie;
    this.molenerf = molenerf;
    this.trivia = trivia;
    this.geschiedenis = geschiedenis;
    this.wetenswaardigheden = wetenswaardigheden;
    this.wederopbouw = wederopbouw;
    this.as = as;
    this.wieken = wieken;
    this.toegangsprijzen = toegangsprijzen;
    this.uniekeEigenschap = uniekeEigenschap;
    this.landschappelijkeWaarde = landschappelijkeWaarde;
    this.kadastraleAanduiding = kadastraleAanduiding;
    this.canAddImages = canAddImages;
    this.eigenaar = eigenaar;
    this.recenteWerkzaamheden = recenteWerkzaamheden;
    this.rad = rad;
    this.radDiameter = radDiameter;
    this.wateras = wateras;
    this.lat = lat;
    this.long = long;
    this.lastUpdated = lastUpdated;
    this.images = images;
    this.addedImages = addedImages || [];
    this.modelType = modelType;
    this.hasImage = hasImage;
  }

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
