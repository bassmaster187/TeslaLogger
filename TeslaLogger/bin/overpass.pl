use strict;

my $baseurl = 'http://overpass-api.de/api/interpreter?data=\[out:xml\];node\[amenity=charging_station\]';
my $urlsuffix = ';out;';
my $apistatus = 'http://overpass-api.de/api/status';
my $start1 = -90;
my $start2 = -180;
my $stop1 = 90;
my $stop2 = 180;
my $step = 4;

my ($regionparam) = @ARGV;

if (defined $regionparam) {
    if ($regionparam eq '--debug') {
        # debug
        $start1 = 48;
        $stop1 = 52;
        $start2 = 10;
        $stop2 = 14;
    }
    elsif ($regionparam eq '--germany') {
        $start1 = 47;
        $stop1 = 55;
        $start2 = 5;
        $stop2 = 16;
    }
}

for (my $i = $start1; $i <= ($stop1 - $step); $i += $step) {
    for (my $j = $start2; $j <= ($stop2 - $step); $j += $step) {
        my $geofence =  "(" . $i . "," . $j . "," . ($i + $step) . "," . ($j + $step) . ")";
        print STDERR "$geofence\n";
        my $url = $baseurl . $geofence . $urlsuffix;
        # check API status
        my $slots = 0;
        my $retries = 10;
        my $attempt = 1;
        while ($slots == 0 && $attempt <= $retries) {
            my $status = `curl $apistatus`;
            print STDERR $status, "\n";
            if ($status =~ /([0-9]+) slots available/s) {
                $slots = $1;
                $attempt++;
                if ($slots == 0) {
                    print STDERR "Slots: $slots, sleep $attempt\n";
                    sleep($attempt);
                }
            }
            else {
                if ($status =~ /Slot available after:.+?in ([0-9]+) seconds/) {
                    my $seconds = $1;
                    print STDERR "Slots: $slots, sleep $seconds\n";
                    sleep($seconds);
                }
                else {
                    print STDERR "status unavailable, sleep 5\n";
                    sleep(5);
                    $attempt++;
                }
            }
        }
        if ($slots > 0) {
            $attempt = 1;
            $slots = 0;
            my $xml = `curl '$url'`;
            foreach my $node ($xml =~ /(<node.+?\/node>)/sg) {
                if ($node =~ /<node.+?lat="(.+)".+?lon="(.+?)">/) {
                    my $lat = $1;
                    my $lon = $2;
                    my $name = "";
                    my $operator = "";
                    my $network = "";
                    foreach my $tag ($node =~ /<tag.+?k="(.+?)".+?v="(.+?)"\/>/g) {
                        my $key = $1;
                        my $value = $2;
                        # clean up value
                        $value =~ s/&amp;//g;
                        $value =~ s/;/ /g;
                        if ($key eq "name") {
                            $name = $value;
                        }
                        elsif ($key eq "operator") {
                            $operator = $value;
                        }
                        elsif ($key eq "network") {
                            $network = $value;
                        }
                    }
                    if ($name && $operator) {
                        print "$name - $operator, $lat, $lon\n";
                    }
                    elsif ($name) {
                        print "$name, $lat, $lon\n";
                    }
                    elsif ($operator) {
                        print "$operator, $lat, $lon\n";
                    }
                    elsif ($network) {
                        print "$network, $lat, $lon\n";
                    }
                    else {
                        print STDERR "no name for $lat $lon\n";
                    }
                }
            }
        }
    }
}
