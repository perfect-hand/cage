import { Routes } from '@angular/router';
import { Home } from './home/home';
import { CreateOrganization } from './organizations/create-organization';
import { Organizations } from './organizations/organizations';

export const routes: Routes = [
	{
		path: '',
		component: Home,
		children: [
			{ path: '', component: Organizations },
			{ path: 'organizations/new', component: CreateOrganization },
		],
	},
];
