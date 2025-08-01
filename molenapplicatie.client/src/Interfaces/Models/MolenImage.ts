export interface MolenImage {
  id: number;
  filePath: string;
  name: string;
  canBeDeleted: boolean;
  dateTaken: Date | undefined;
  description: string;
  molenDataId: number;
  isAddedImage: boolean;
}
