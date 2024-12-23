export class MolenMaker {
  id: number;
  name: string;
  year: string;
  molenDataId: number;

  constructor(id: number, name: string, year: string, molenDataId: number) {
    this.id = id;
    this.name = name;
    this.year = year;
    this.molenDataId = molenDataId;
  }
}
