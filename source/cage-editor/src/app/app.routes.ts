import { Routes } from '@angular/router';
import { Home } from './home/home';
import { CreateOrganization } from './organizations/create-organization';
import { EditOrganization } from './organizations/edit-organization';
import { CreateTitle } from './titles/create-title';
import { EditTitle } from './titles/edit-title';
import { Titles } from './titles/titles';
import { Organizations } from './organizations/organizations';

export const routes: Routes = [
  {
    path: '',
    component: Home,
    children: [
      { path: '', component: Organizations },
      { path: 'organizations/new', component: CreateOrganization },
      { path: 'organizations/:organizationId/edit', component: EditOrganization },
      { path: 'organizations/:organizationId/titles', component: Titles },
      { path: 'organizations/:organizationId/titles/new', component: CreateTitle },
      { path: 'organizations/:organizationId/titles/:titleId/edit', component: EditTitle },
    ],
  },
];
