namespace BusinessSearch.wwwroot.js
{
    $(document).ready(function () {
        // Add a click handler for the Save Assignment button
        $('#assignListBtn').on('click', function (e) {
            e.preventDefault();

            const id = $('#id').val();
            const assignedToId = $('#assignedToId').val();

            console.log('Updating assignment:', { id, assignedToId });

            // Send the update via AJAX
            $.ajax({
                url: '/Crm/AssignList',
                type: 'POST',
                data: {
                    id: id,
                    assignedToId: assignedToId,
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (response) {
                    console.log('Server response:', response);
                    if (response.success) {
                        alert('Assignment updated successfully');
                        window.location.href = '/Crm/Index';
                    } else {
                        alert('Error: ' + response.message);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('AJAX Error:', xhr.responseText);
                    alert('An unexpected error occurred. Please try again.');
                }
            });

            // Add this to your assign-list.js file
            $(document).ready(function () {
                // Get the actual User ID that corresponds to the selected TeamMember
                function getCorrespondingUserId(teamMemberId) {
                    // You can do an AJAX call to get the correct User ID
                    // Or simply have a dictionary/mapping

                    // For now, we'll just return null to unassign
                    if (!teamMemberId) return '';

                    // This is the key issue - you need to get the corresponding USER ID
                    // not the TeamMember ID
                    return teamMemberId;
                }

                $('#assignListBtn').on('click', function (e) {
                    e.preventDefault();

                    var listId = $('#id').val();
                    var teamMemberId = $('#assignedToId').val();

                    // Get the corresponding USER ID
                    var userId = getCorrespondingUserId(teamMemberId);

                    // Get anti-forgery token
                    var token = $('input[name="__RequestVerificationToken"]').val();

                    // Make AJAX request
                    $.ajax({
                        url: '/Crm/AssignUser',
                        type: 'POST',
                        data: {
                            listId: listId,
                            userId: userId,
                            __RequestVerificationToken: token
                        },
                        success: function (response) {
                            if (response.success) {
                                alert('Assignment updated successfully');
                                window.location.href = '/Crm/Index';
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
            });
        });
    });
}
