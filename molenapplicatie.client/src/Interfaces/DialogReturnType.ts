import { DialogReturnStatus } from "../Enums/DialogReturnStatus";

export interface DialogReturnType {
  status: DialogReturnStatus;
  message?: string;
  api_key?: string;
}
