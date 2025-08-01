import { ToastType } from '../Enums/ToastType';

export interface Toast {
  id: number;
  message: string;
  type: ToastType;
  duration?: number;
}
