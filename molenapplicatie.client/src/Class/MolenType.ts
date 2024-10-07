export class MolenType {
  id: number; // Primary key, auto-increment
  name: string;

  constructor(id: number, name: string) {
    this.id = id;
    this.name = name;
  }
}
