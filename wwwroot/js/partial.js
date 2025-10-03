('#WardId').change(function () {
    var wardId = $(this).val();
    $.getJSON('/Admission_Descharge/GetRooms', { wardId }, function (rooms) {
        var roomSelect = $('#RoomId');
        roomSelect.empty().append('<option value="">-- Select Room --</option>');
        $.each(rooms, function (i, room) { roomSelect.append('<option value="' + room.roomId + '">' + room.roomNumber + '</option>'); });
        $('#BedId').empty().append('<option value="">-- Select Bed --</option>');
    });
});

('#RoomId').change(function () {
    var roomId = $(this).val();
    $.getJSON('/Admission_Descharge/GetBeds', { roomId }, function (beds) {
        var bedSelect = $('#BedId');
        bedSelect.empty().append('<option value="">-- Select Bed --</option>');
        $.each(beds, function (i, bed) { bedSelect.append('<option value="' + bed.bedId + '">' + bed.bedNumber + '</option>'); });
    });
});