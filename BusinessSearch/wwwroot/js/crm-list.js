namespace BusinessSearch.wwwroot.js
{
    $(document).ready(function () {
        // Add this function for the "Update Assignment Only" button
        $('#assignUserBtn').on('click', function (e) {
            e.preventDefault();

            const id = $('#editListId').val();
            const assignedToId = $('#editListAssignedTo').val();

            console.log('Assigning user to list:', { id, assignedToId });

            // Get the token
            const token = $('input[name="__RequestVerificationToken"]').val();

            // Make the request
            $.ajax({
                url: '/Crm/AssignUser',
                type: 'POST',
                data: {
                    id: id,
                    assignedToId: assignedToId,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    console.log('Response:', response);
                    if (response.success) {
                        alert('Assignment updated successfully');
                        location.reload();
                    } else {
                        alert('Error: ' + response.message);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('AJAX Error:', xhr.responseText);
                    alert('An unexpected error occurred. Please try again.');
                }
            });
        });

        // Override the saveListChanges function with a more direct approach
        window.saveListChanges = function () {
            const id = $('#editListId').val();
            const name = $('#editListName').val().trim();
            const description = $('#editListDescription').val().trim();
            const industry = $('#editListIndustry').val().trim();
            const assignedToId = $('#editListAssignedTo').val();

            if (!name) {
                alert('List name is required');
                return;
            }

            console.log('Saving list changes:', { id, name, description, industry, assignedToId });

            // Get the token
            const token = $('input[name="__RequestVerificationToken"]').val();

            // Make the request to AssignUser instead (simpler approach)
            $.ajax({
                url: '/Crm/AssignUser',
                type: 'POST',
                data: {
                    id: id,
                    assignedToId: assignedToId,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    if (response.success) {
                        // Now update the other fields separately
                        updateListDetails(id, name, description, industry, token);
                    } else {
                        alert('Error updating assignment: ' + response.message);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('AJAX Error:', xhr.responseText);
                    alert('An unexpected error occurred while updating assignment. Please try again.');
                }
            });
        };

        // Function to update list details without changing assignment
        function updateListDetails(id, name, description, industry, token) {
            $.ajax({
                url: '/Crm/UpdateListDetails',
                type: 'POST',
                data: {
                    id: id,
                    name: name,
                    description: description,
                    industry: industry,
                    __RequestVerificationToken: token
                },
                success: function (response) {
                    if (response.success) {
                        alert('List updated successfully');
                        location.reload();
                    } else {
                        alert('Error updating list details: ' + response.message);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('AJAX Error:', xhr.responseText);
                    alert('An unexpected error occurred while updating list details. Please try again.');
                }
            });
        }
    });
}
