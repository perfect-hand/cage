import { Routes } from '@angular/router';
import { Home } from './home/home';
import { CreateOrganization } from './organizations/create-organization';
import { EditOrganization } from './organizations/edit-organization';
import { CreateTitle } from './titles/create-title';
import { EditTitle } from './titles/edit-title';
import { CreateTitleVersion } from './titles/create-title-version';
import { EditTitleVersion } from './titles/edit-title-version';
import { Attributes } from './attributes/attributes';
import { CreateAttribute } from './attributes/create-attribute';
import { EditAttribute } from './attributes/edit-attribute';
import { Tags } from './tags/tags';
import { CreateTag } from './tags/create-tag';
import { EditTag } from './tags/edit-tag';
import { Titles } from './titles/titles';
import { TitleVersions } from './titles/title-versions';
import { TitleVersionOverview } from './titles/title-version-overview';
import { Organizations } from './organizations/organizations';
import { PrivacyPolicy } from './privacy-policy/privacy-policy';

export const routes: Routes = [
  { path: 'privacy-policy', component: PrivacyPolicy },
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
      { path: 'organizations/:organizationId/titles/:titleId/versions/new', component: CreateTitleVersion },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId/edit', component: EditTitleVersion },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId', component: TitleVersionOverview },
      { path: 'organizations/:organizationId/titles/:titleId/versions', component: TitleVersions },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId/attributes', component: Attributes },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId/attributes/new', component: CreateAttribute },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId/attributes/:attributeId/edit', component: EditAttribute },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId/tags', component: Tags },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId/tags/new', component: CreateTag },
      { path: 'organizations/:organizationId/titles/:titleId/versions/:versionId/tags/:tagId/edit', component: EditTag },
    ],
  },
];
