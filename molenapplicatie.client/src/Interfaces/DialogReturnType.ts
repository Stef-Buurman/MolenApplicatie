import { DialogReturnStatus } from "../Enums/DialogReturnStatus";

export interface DialogReturnType {
  status: DialogReturnStatus;
  message?: string;
}
