import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MapRemainderMolensComponent } from './map-remainder-molens.component';

describe('MapRemainderMolensComponent', () => {
  let component: MapRemainderMolensComponent;
  let fixture: ComponentFixture<MapRemainderMolensComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [MapRemainderMolensComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MapRemainderMolensComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
