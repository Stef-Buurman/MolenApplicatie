import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ImageRouteComponent } from './image-route/image-route.component';
import { OpenMolenDetailsComponent } from './open-molen-details/open-molen-details.component';
import { MapPageComponent } from './map-page/map-page.component';

const routes: Routes = [
  { path: '', redirectTo: '/map', pathMatch: 'full' },
  {
    path: 'map',
    component: MapPageComponent,
    children: [
      {
        path: ':MolenId',
        component: OpenMolenDetailsComponent,
        children: [
          {
            path: ':imageName',
            component: ImageRouteComponent,
          },
        ],
      },
    ],
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
