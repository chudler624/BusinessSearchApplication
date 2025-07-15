namespace BusinessSearch.wwwroot.js
{
    public class list_edit
    {
        $(document).ready(function() {
            // Handle the form submission for list editing
            $('#listEditForm').on('submit', function (e) {
                e.preventDefault();

                // Get form values
                var id = $('#id').val();
                var name = $('#name').val();
                var description = $('#description').val();
                var industry = $('#industry').val();
                var assignedToId = $('#assignedToId').val();

                // Log values for debugging
                console.log('Form submission values:');
                console.log('id:', id);
                console.log('name:', name);
                console.log('description:', description);
                console.log('industry:', industry);
                console.log('assignedToId:', assignedToId);

                // Create form data for submission
                var formData = {
                    id: id,
                    name: name,
                    description: description,
                    industry: industry,
                    assignedToId: assignedToId,
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                };

                // Submit the form via AJAX
                // Submit the form via AJAX
                $.ajax({
                    url: '/Crm/UpdateList',
                    type: 'POST',
                    data: {
                        id: parseInt(id),  // Ensure it's a number
                        name: name,
                        description: description || "",
                        industry: industry || "",
                        assignedToId: assignedToId,  // Keep this as is
                        __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                    },
                    success: function (response) {
                        console.log("Server response:", response);
                        if (response.success) {
                            // Show success message and details
                            console.log("List updated with assignedToId:", response.assignedToId);
                            alert('List updated successfully');

                            // Redirect back to the list index after a short delay
                            setTimeout(function () {
                                window.location.href = '/Crm/Index';
                            }, 1000);
                        } else {
                            // Show detailed error message
                            console.error("Update failed:", response.message);
                            alert(response.message || 'An error occurred while updating the list');
                        }
                    },
                    error: function (xhr, status, error) {
                        console.error('AJAX Error:', xhr.status, xhr.responseText);
                        alert('An unexpected error occurred. Please check the console for details.');
                    }
                });

                // Add this to your list-edit.js file
                function updateAssignedTo(id, assignedToId) {
                    // Get anti-forgery token
                    var token = $('input[name="__RequestVerificationToken"]').val();

                    // Send only the specific fields we need
                    $.ajax({
                        url: '/Crm/DirectUpdate',
                        type: 'POST',
                        data: {
                            id: id,
                            assignedToId: assignedToId,
                            __RequestVerificationToken: token
                        },
                        success: function (response) {
                            console.log("Response:", response);
                            if (response.success) {
                                alert('List updated successfully');
                                window.location.href = '/Crm/Index';
                            } else {
                                alert('Error: ' + response.message);
                            }
                        },
                        error: function (xhr, status, error) {
                            console.error('Error:', error);
                            alert('An unexpected error occurred');
                        }
                    });
                }

                // Add this at the end of your document ready function
                $('#updateAssignedToBtn').on('click', function () {
                    var id = $('#id').val();
                    var assignedToId = $('#assignedToId').val();
                    updateAssignedTo(id, assignedToId);
                });

                // Add to your existing list-edit.js or create this as assign-list.js
                $(document).ready(function () {
                    // Handle the dropdown change directly
                    $('#assignedToId').on('change', function () {
                        const listId = $('#id').val();
                        const assignedToId = $(this).val();

                        console.log('Updating assignment:', { listId, assignedToId });

                        // Get anti-forgery token
                        const token = $('input[name="__RequestVerificationToken"]').val();

                        // Send the update via AJAX
                        $.ajax({
                            url: '/Crm/AssignList',
                            type: 'POST',
                            data: {
                                id: listId,
                                assignedToId: assignedToId,
                                __RequestVerificationToken: token
                            },
                            success: function (response) {
                                console.log('Server response:', response);
                                if (response.success) {
                                    // Show success message (optional)
                                    alert('Assignment updated successfully');
                                } else {
                                    // Show error message
                                    alert('Error: ' + response.message);
                                }
                            },
                            error: function (xhr, status, error) {
                                console.error('AJAX Error:', xhr.responseText);
                                alert('An unexpected error occurred. Please try again.');
                            }
                        });
                    });

                    // Add a separate button to explicitly save the assignment
                    $('#saveAssignmentBtn').on('click', function () {
                        const listId = $('#id').val();
                        const assignedToId = $('#assignedToId').val();

                        // Get anti-forgery token
                        const token = $('input[name="__RequestVerificationToken"]').val();

                        $.ajax({
                            url: '/Crm/AssignList',
                            type: 'POST',
                            data: {
                                id: listId,
                                assignedToId: assignedToId,
                                __RequestVerificationToken: token
                            },
                            success: function (response) {
                                console.log('Server response:', response);
                                if (response.success) {
                                    // Show success message and redirect
                                    alert('Assignment updated successfully');
                                    window.location.href = '/Crm/Index';
                                } else {
                                    // Show error message
                                    alert('Error: ' + response.message);
                                }
                            },
                            error: function (xhr, status, error) {
                                console.error('AJAX Error:', xhr.responseText);
                                alert('An unexpected error occurred. Please try again.');
                            }
                        });
                    });
                });
            });
        });
    }
}
