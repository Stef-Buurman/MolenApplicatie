import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { FilterFormValues } from '../../../Interfaces/Filters/Filter';
import { Toasts } from '../../../Utils/Toasts';
import { MolenFilterList } from '../../../Interfaces/Filters/MolenFilterList';
import { MolenFilters } from '../../../Interfaces/Filters/MolenFilters';
import { MolenService } from '../../../Services/MolenService';

@Component({
  selector: 'app-filter-map',
  templateUrl: './filter-map.component.html',
  styleUrl: './filter-map.component.scss',
})
export class FilterMapComponent implements OnInit {
  selectedFilter: MolenFilters = {
    provincie: '',
    toestand: '',
    type: '',
  };
  provincie: string = '';
  molenFilters: MolenFilterList = { provincies: [], toestanden: [], types: [] };
  filters: { [name: string]: FilterFormValues } = {};
  constructor(
    private dialogRef: MatDialogRef<FilterMapComponent>,
    @Inject(MAT_DIALOG_DATA) public data: { filters: FilterFormValues[] },
    private toasts: Toasts,
    private molenService: MolenService
  ) {}

  ngOnInit() {
    let filters = this.data.filters || [];
    filters.forEach((filter) => {
      this.filters[filter.filterName] = filter;
    });
    if (this.filters['Provincie']) {
      const provincieValue = this.filters['Provincie'].value;
      this.selectedFilter.provincie =
        typeof provincieValue === 'string'
          ? provincieValue.toLowerCase()
          : undefined;
    }
    if (this.filters['MolenState']) {
      const toestandValue = this.filters['MolenState'].value;
      this.selectedFilter.toestand =
        typeof toestandValue === 'string'
          ? toestandValue.toLowerCase()
          : undefined;
    }
    if (this.filters['MolenType']) {
      const molenTypeValue = this.filters['MolenType'].value;
      this.selectedFilter.type =
        typeof molenTypeValue === 'string'
          ? molenTypeValue.toLowerCase()
          : undefined;
    }
    this.molenService.getAllMolenFilters().subscribe({
      next: (filters: MolenFilterList) => {
        this.molenFilters = filters;
      },
    });
  }

  onClose(filters: FilterFormValues[] | undefined = undefined) {
    this.dialogRef.close(filters);
  }

  filterMap() {
    if (this.selectedFilter.toestand) {
      if (!this.filters['MolenState']) {
        this.filters['MolenState'] = {
          filterName: 'MolenState',
          value: this.selectedFilter.toestand || '',
          type: 'string',
          isAList: false,
          name: 'Molen state',
        };
      } else {
        this.filters['MolenState'].value = this.selectedFilter.toestand || '';
      }
    } else {
      delete this.filters['MolenState'];
    }

    if (this.selectedFilter.provincie) {
      if (!this.filters['Provincie']) {
        this.filters['Provincie'] = {
          filterName: 'Provincie',
          value: this.selectedFilter.provincie || '',
          type: 'string',
          isAList: false,
          name: 'Provincie',
        };
      } else {
        this.filters['Provincie'].value = this.selectedFilter.provincie || '';
      }
    } else {
      delete this.filters['Provincie'];
    }

    if (this.selectedFilter.type) {
      if (!this.filters['MolenType']) {
        this.filters['MolenType'] = {
          filterName: 'MolenType',
          value: this.selectedFilter.type || '',
          type: 'string',
          isAList: false,
          name: 'Molen type',
        };
      } else {
        this.filters['MolenType'].value = this.selectedFilter.type || '';
      }

      if (
        !this.selectedFilter.provincie &&
        !this.selectedFilter.toestand &&
        (this.molenFilters.types.find(
          (t) => t.name.toLowerCase() === this.selectedFilter.type
        )?.count ?? 0) > 1100
      ) {
        this.filters['MolenState'] = {
          filterName: 'MolenState',
          value: 'werkend',
          type: 'string',
          isAList: false,
          name: 'Molen state',
        };
      }
    } else {
      delete this.filters['MolenType'];
    }

        if (
      this.filters['MolenState'] &&
      !this.filters['Provincie'] &&
      !this.filters['MolenType'] &&
      (this.selectedFilter.toestand === 'verdwenen' ||
        this.filters['MolenState'].value === 'verdwenen')
    ) {
      this.toasts.showInfo('Je hebt geen provincie gekozen!');
      return;
    }

    this.onClose(Object.values(this.filters));
  }
}
