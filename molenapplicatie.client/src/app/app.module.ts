import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogModule } from '@angular/material/dialog';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { MolenDialogComponent } from './dialogs/molen-dialog/molen-dialog.component';
import { FormsModule } from '@angular/forms';
import { ImageSelectorComponent } from './image-selector/image-selector.component';
import { ImageDialogComponent } from './dialogs/image-dialog/image-dialog.component';
import { ToastrModule } from 'ngx-toastr';
import { ConfirmationDialogComponent } from './dialogs/confirmation-dialog/confirmation-dialog.component';
import { ToastComponent } from './toast/toast.component';
import { LoaderComponent } from './loader/loader.component';
import { ErrorMessageComponent } from './error-message/error-message.component';
import { DropdownComponent } from './dropdown/dropdown.component';
import { RootComponent } from './root/root.component';
import { OpenMolenDetailsComponent } from './open-molen-details/open-molen-details.component';
import { FilterMapComponent } from './dialogs/filter-map/filter-map.component';
import { UploadImageDialogComponent } from './dialogs/upload-image-dialog/upload-image-dialog.component';
import { MapPageComponent } from './map-page/map-page.component';

@NgModule({
  declarations: [
    AppComponent,
    MolenDialogComponent,
    ImageSelectorComponent,
    ImageDialogComponent,
    ConfirmationDialogComponent,
    ToastComponent,
    LoaderComponent,
    ErrorMessageComponent,
    DropdownComponent,
    RootComponent,
    OpenMolenDetailsComponent,
    FilterMapComponent,
    UploadImageDialogComponent,
    MapPageComponent,
  ],
  imports: [
    BrowserAnimationsModule,
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    MatDialogModule,
    FormsModule,
    ToastrModule.forRoot(),
  ],
  providers: [],
  bootstrap: [AppComponent],
})
export class AppModule {}
