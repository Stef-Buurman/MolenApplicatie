import { MolenData, MolenImage } from '../api/generated/data-contracts';

export interface MolensResponseType<T> {
  activeMolensWithImage: number;
  remainderMolensWithImage: number;
  totalMolensWithImage: number;
  totalCountActiveMolens: number;
  totalCountRemainderMolens: number;
  totalCountDisappearedMolens: CountDisappearedMolens[];
  totalCountExistingMolens: number;
  totalCountMolens: number;
  molens: T[];
  recentAddedImages?: RecentAddedImages[];
}

export interface CountDisappearedMolens {
  provincie: string;
  count: number;
}

export interface RecentAddedImages {
  molen: MolenData;
  images: MolenImage[];
}
