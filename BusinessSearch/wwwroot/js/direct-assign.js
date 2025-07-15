namespace BusinessSearch.wwwroot.js
{
    $(document).ready(function () {
        // Add a click handler to Save Changes button that uses our direct method
        $('#saveChangesBtn').on('click', function (e) {
            e.preventDefault();

            const id = $('#id').val();
            const assignedToId = $('#assignedToId').val();

            console.log('Directly assigning list:', { id, assignedToId });

            // Get token
            const token = $('input[name="__RequestVerificationToken"]').val();

            // Send request to our direct assign method
            $.ajax({
                url: '/Crm/DirectAssign',
                type: 'POST',
                data: {
                    id: id,
                    teamMemberId: assignedToId,
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
}
