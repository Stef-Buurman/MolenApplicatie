export interface ConfirmationDialogData {
  title: string;
  message: string;
  api_key_usage: boolean;
  onConfirm: (api_key?: string) => void;
  onDeny?: () => void;
}
