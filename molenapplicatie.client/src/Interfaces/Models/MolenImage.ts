export interface MolenImage {
  id: string;
  filePath: string;
  name: string;
  canBeDeleted: boolean;
  dateTaken?: string | Date | null;
  description?: string | null;
  molenDataId: string;
  externalUrl?: string;
  isAddedImage?: boolean;
}
