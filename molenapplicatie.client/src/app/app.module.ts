import { HttpClientModule } from '@angular/common/http';
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogModule } from '@angular/material/dialog';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { MapComponent } from './map/map.component';
import { MolenDialogComponent } from './dialogs/molen-dialog/molen-dialog.component';
import { FormsModule } from '@angular/forms';
import { ImageSelectorComponent } from './image-selector/image-selector.component';
import { ImageDialogComponent } from './dialogs/image-dialog/image-dialog.component';
import { ToastrModule } from 'ngx-toastr';
import { ConfirmationDialogComponent } from './dialogs/confirmation-dialog/confirmation-dialog.component';
import { ToastComponent } from './toast/toast.component';
import { Toasts } from '../Utils/Toasts';
import { LoaderComponent } from './loader/loader.component';
import { ErrorMessageComponent } from './error-message/error-message.component';
import { DropdownComponent } from './dropdown/dropdown.component';
import { RootComponent } from './root/root.component';
import { ErrorService } from '../Services/ErrorService';
import { OpenMolenDetailsComponent } from './open-molen-details/open-molen-details.component';
import { MapActiveMolensComponent } from './map-active-molens/map-active-molens.component';
import { MapExistingMolensComponent } from './map-existing-molens/map-existing-molens.component';
import { MapRemainingMolensComponent } from './map-remaining-molens/map-remaining-molens.component';
import { MapDisappearedMolensComponent } from './map-disappeared-molens/map-disappeared-molens.component';
import { MolensRootActiveComponent } from './molens-root-active/molens-root-active.component';
import { FilterMapComponent } from './filter-map/filter-map.component';
import { UploadImageDialogComponent } from './dialogs/upload-image-dialog/upload-image-dialog.component';

@NgModule({
  declarations: [
    AppComponent,
    MapComponent,
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
    MapActiveMolensComponent,
    MapExistingMolensComponent,
    MapRemainingMolensComponent,
    MapDisappearedMolensComponent,
    MolensRootActiveComponent,
    FilterMapComponent,
    UploadImageDialogComponent
  ],
  imports: [
    BrowserAnimationsModule,
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,
    MatDialogModule,
    FormsModule,
    ToastrModule.forRoot()
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
