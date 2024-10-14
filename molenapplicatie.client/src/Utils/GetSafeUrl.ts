import { DomSanitizer, SafeUrl } from "@angular/platform-browser";
export function GetSafeUrl(sanitizer: DomSanitizer, data: Uint8Array): SafeUrl {
  let objectURL = 'data:image/png;base64,' + data;
  return sanitizer.bypassSecurityTrustUrl(objectURL);
}
