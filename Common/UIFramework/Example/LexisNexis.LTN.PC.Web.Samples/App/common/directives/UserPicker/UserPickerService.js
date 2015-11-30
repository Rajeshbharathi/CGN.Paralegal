(function () {
    "use strict";

    function UserPickerService(WebApiClientService) {

        function getUserGroupsWithUsers(organizationId) {
            return WebApiClientService.get('/api/userpicker/usergroupsandusers/' + organizationId);
        }

        function getUserGroups(organizationId) {
            return WebApiClientService.get('/api/userpicker/usergroups/' + organizationId);
        }

        function getUser(organizationId) {
            return WebApiClientService.get('/api/userpicker/users/' + organizationId);
        }

        function postFilterUserGroupsAndUsers(organizationId, filter) {
            return WebApiClientService.post('/api/userpicker/usergroupswithusers/filter/' + organizationId, filter);
        }

        /* Start : UserPickerController - Helper method  */
        function constructUserList(users) {
            var result = [],
                len,
                index,
                total,
                subTotal;
            if (users.length > 0) {
                total = users.length;
                for (len = 0; len < total; len = len + 1) {
                    result.push(users[len].UserName);
                    if (!angular.isUndefined(users[len].Users) &&
                            users[len].Users !== null) {
                        subTotal = users[len].Users.length;
                        for (index = 0; index < subTotal; index = index + 1) {
                            result.push(users[len].Users[index].UserName);
                        }
                    }
                }
            }
            return result;
        }
        function removeUsersFromUserList(userList, selectedUsers) {

            var users = userList,
                ulen,
                len,
                total = users.length,
                removeCount = selectedUsers.length;
            for (ulen = 0; ulen < removeCount; ulen = ulen + 1) {
                for (len = 0; len < total; len = len + 1) {

                    if (users[len].UserId === selectedUsers[ulen].UserId) {
                        userList.splice(len, 1);
                        break;
                    }
                }
            }
            return userList;
        }
        /* End : UserPickerController - Helper method  */

        /* Start : UserPickerDialogController - Helper method  */
        function constructAndSetStateOnData(response, selectedUsers) {
            var responseData = response,
                len,
                index,
                dataCount,
                num,
                sIndex,
                ulen,
                parent,
                hasChild,
                count,
                dataLength,
                sUserLen,
                pUserLen;
            //Set Group Id for sub users.
            for (len = 0; len < responseData.length; len = len + 1) {
                parent = responseData[len];
                hasChild = (!angular.isUndefined(parent.Users));
                if (hasChild) {
                    count = parent.Users.length;
                    for (index = 0; index < count; index = index + 1) {
                        parent.Users[index].Groupid = parent.UserId;
                    }
                }
            }
            //For Retain previous selection
            if (!angular.isUndefined(selectedUsers)) {

                for (dataCount = 0; dataCount < responseData.length; dataCount = dataCount + 1) {
                    //Root
                    dataLength = selectedUsers.length;
                    for (num = 0; num < dataLength; num = num + 1) {
                        if (responseData[dataCount].UserId === selectedUsers[num].UserId) {
                            responseData[dataCount].checked = true;
                            break;
                        }
                    }

                    //Sub users
                    if (!angular.isUndefined(responseData[dataCount].Users) &&
                            responseData[dataCount].Users.length > 0) {
                        sUserLen = responseData[dataCount].Users.length;
                        for (sIndex = 0; sIndex <
                                sUserLen; sIndex = sIndex + 1) {
                            pUserLen = selectedUsers.length;
                            for (ulen = 0; ulen < pUserLen; ulen = ulen + 1) {
                                if (responseData[dataCount].Users[sIndex].UserId === selectedUsers[ulen].UserId) {
                                    responseData[dataCount].Users[sIndex].checked = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return responseData;
        }
        function addOrRemoveAllUser(dataItem, selectedUsersList) {
            var rootuser = [],
                index,
                ulen,
                len,
                uIndex,
                usersList,
                slen,
                subuser,
                userExists,
                total;
            if (dataItem.checked) {
                rootuser.UserId = dataItem.UserId;
                rootuser.UserName = dataItem.UserName;
                rootuser.isGroup = true;
                selectedUsersList.push(rootuser);
                usersList = dataItem.Users;
                total = usersList.length;
                for (index = 0; index < total; index = index + 1) {
                    userExists = false;
                    subuser = [];
                    subuser.UserId = usersList[index].UserId;
                    subuser.UserName = usersList[index].UserName;
                    subuser.Groupid = dataItem.UserId;
                    slen = selectedUsersList.length;
                    if (slen > 0) {
                        for (ulen = 0; ulen < slen; ulen = ulen + 1) {

                            if (selectedUsersList[ulen].UserId === subuser.UserId) {
                                userExists = true;
                                break;
                            }
                        }
                    }
                    if (!userExists) {
                        selectedUsersList.push(subuser);
                    }
                }
            } else { //Un Select
                slen = selectedUsersList.length;
                for (len = 0; len < slen; len = len + 1) {

                    if (selectedUsersList[len].UserId === dataItem.UserId) {
                        selectedUsersList.splice(len, 1);
                        break;
                    }
                }
                for (uIndex = 0; uIndex <= slen; 0) {
                    if (selectedUsersList[uIndex].Groupid === dataItem.UserId) {
                        selectedUsersList.splice(uIndex, 1);
                        if (uIndex > 0) {
                            uIndex = uIndex - 1;
                        }
                    } else {
                        uIndex = uIndex + 1;
                    }
                }
            }
            return selectedUsersList;
        }
        function addOrRemoveUser(dataItem, selectedUsersList) {
            var len,
                index,
                user = [],
                total;
            if (dataItem.checked) {
                user.UserId = dataItem.UserId;
                user.UserName = dataItem.UserName;
                user.Groupid = dataItem.Groupid;
                selectedUsersList.push(user);
            } else {
                total = selectedUsersList.length;
                //Unselect selection
                for (len = 0; len < total; len = len + 1) {
                    if (selectedUsersList[len].UserId === dataItem.UserId) {
                        selectedUsersList.splice(len, 1);
                        break;
                    }
                }
                //Unselect root node also
                for (index = 0; index < total; index = index + 1) {

                    if (selectedUsersList[index].UserId === dataItem.Groupid) {
                        selectedUsersList.splice(index, 1);
                        break;
                    }
                }
            }
            return selectedUsersList;
        }
        /* End : UserPickerDialogController - Helper method  */
        var service = {
            getUserGroupsWithUsers: getUserGroupsWithUsers,
            getUserGroups: getUserGroups,
            getUser: getUser,
            postFilterUserGroupsAndUsers: postFilterUserGroupsAndUsers,
            constructUserList: constructUserList,
            removeUsersFromUserList: removeUsersFromUserList,
            constructAndSetStateOnData: constructAndSetStateOnData,
            addOrRemoveAllUser: addOrRemoveAllUser,
            addOrRemoveUser: addOrRemoveUser
        };
        return service;
    }
    angular.module('app')
        .factory('UserPickerService', UserPickerService);
    UserPickerService.$inject = ['WebApiClientService'];
}());