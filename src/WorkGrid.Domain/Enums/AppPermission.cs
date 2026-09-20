namespace WorkGrid.Domain.Enums;

public enum AppPermission
{
    // Employee permissions
    EmployeeView = 101,
    EmployeeCreate = 102,
    EmployeeEdit = 103,
    EmployeeDelete = 104,

    // Asset permissions
    AssetView = 201,
    AssetCreate = 202,
    AssetEdit = 203,
    AssetDelete = 204,
    AssetMaintenance = 205,
    AssetRetire = 206,

    // Assignment permissions
    AssignmentView = 301,
    AssignmentCreate = 302,
    AssignmentReturn = 303,

    // User Administration permissions
    UserView = 401,
    UserCreate = 402,
    UserEdit = 403,
    UserDeactivate = 404
}
