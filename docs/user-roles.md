# DevFlow — User Roles and Permissions

## 1. Overview

DevFlow uses role-based and policy-based authorization.

Permissions depend on:

* User authentication status.
* Organization membership.
* Organization role.
* Project membership.
* Resource ownership.

---

## 2. System Roles

### 2.1 System Administrator

The System Administrator manages platform-level operations.

Permissions include:

* View users.
* View organizations.
* Monitor system activity.
* Access administrative reports.
* View application health information.
* Perform authorized administrative operations.

System Administrators do not automatically participate in organization projects.

---

## 3. Organization Roles

### 3.1 Organization Owner

The Organization Owner has the highest level of authority within an organization.

Permissions include:

* View organization.
* Update organization.
* Manage organization members.
* Assign organization roles.
* Create teams.
* Update teams.
* Delete or deactivate teams.
* Create projects.
* Update projects.
* Archive projects.
* Manage project membership.

Every organization must have at least one owner.

---

### 3.2 Organization Admin

The Organization Admin assists with organization management.

Permissions include:

* View organization.
* Manage organization members where permitted.
* Create teams.
* Update teams.
* Manage team membership.
* Create projects.
* Manage projects.

Organization Admins cannot perform owner-only operations.

---

### 3.3 Organization Member

An Organization Member is a standard member of an organization.

Permissions include:

* View authorized organization information.
* View teams they are allowed to access.
* Access projects where they have membership or permission.
* Create and update issues where permitted.
* Add comments.
* Participate in sprints.

---

## 4. Project Roles

### 4.1 Project Manager

Permissions include:

* Manage project information.
* Manage project members.
* Assign teams.
* Create issues.
* Assign issues.
* Manage sprints.
* Start sprints.
* Complete sprints.
* Manage issue priorities and statuses.

---

### 4.2 Developer

Permissions include:

* View assigned projects.
* View issues.
* Create issues where permitted.
* Update assigned issues.
* Add comments.
* Update issue statuses.
* Participate in sprints.

---

## 5. Authorization Principles

DevFlow shall follow these authorization principles:

* Authentication alone does not grant access to organization resources.
* Users must belong to an organization before accessing its protected resources.
* Organization roles shall control organization-level operations.
* Project membership shall control access to project resources.
* Resource ownership and membership shall be verified by backend authorization policies.
* Frontend route guards shall improve user experience but shall not replace backend authorization.
* Every protected operation must be authorized by the backend API.

---

## 6. Initial Role Model

The initial implementation shall support:

### Platform Level

* System Administrator

### Organization Level

* Owner
* Admin
* Member

### Project Level

* Project Manager
* Developer

The role model may be expanded after the MVP is completed.
