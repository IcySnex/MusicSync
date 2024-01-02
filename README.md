# MusicSync

### Synchronize Music from iTunes to Android
This tool allows you to synchronize an entire folder of music with your Android device.
It is also able to synchronize your entire iTunes library, including playlists and ratings, to your Android device and vice versa.
This enables seamless syncing between Android and iTunes without the need for any third-party software on your phone.

----

### How it works
Synchronizing your music library is straightforward. The tool loops over every MP3 file in a specified directory and uses ADB for seamless and fast transferring.

To sync playlists and ratings from iTunes to the default Android music player, considerable development was required.
Initially, an understanding of how the default Android music player operates was necessary. Android stores all media (music, images, videos, etc.) in a SQLite 3 database, which is only accessible through root services and is located at `/data/data/com.android.providers.media/databases/external.db`.
By using a rooted ADB service, we can pull that file, parse the entire library, and modify and extend the database as needed.
The subsequent steps involve using the iTunes COM SDK for Windows to loop over every playlist, adding it along with all its tracks to Android, pushing the file back to the device, and forcing a media refresh through ADB.

Syncing back from Android to iTunes is even easier. Just pull the Android Media Library database and reverse the aforementioned steps.

----

### Notes
This tool has only been tested with Android 4.0.4 on a Samsung Galaxy S Duos.
There is uncertainty about its compatibility with newer versions of Android.
Ensure that iTunes x64 is installed and not the Microsoft Store version.

_**Use at your own risk; This tool modifies and deletes data in both Android and iTunes. I am not liable for any data loss.**_
