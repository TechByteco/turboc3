#include <stdio.h>
#include <conio.h>

void main() {
    int ref[50], frame[10], last[10];
    int n, f, i, j, pos, faults = 0;

    clrscr();
    printf("Enter number of pages: ");
    scanf("%d", &n);

    printf("Enter reference string: ");
    for (i = 0; i < n; i++) {
        scanf("%d", &ref[i]);
    }

    printf("Enter number of frames: ");
    scanf("%d", &f);

    for (j = 0; j < f; j++) {
        frame[j] = -1;
        last[j] = -1;
    }

    printf("\n--- LRU Page Replacement Simulation ---\n");

    for (i = 0; i < n; i++) {
        pos = -1;
        // Check if page is already in a frame (Hit)
        for (j = 0; j < f; j++) {
            if (frame[j] == ref[i]) {
                pos = j;
                break;
            }
        }

        // Page Fault occurred
        if (pos == -1) {
            faults++;
            // Check for an empty frame first
            for (j = 0; j < f; j++) {
                if (frame[j] == -1) {
                    pos = j;
                    break;
                }
            }
            // If no empty frame, find least recently used frame
            if (pos == -1) {
                pos = 0;
                for (j = 1; j < f; j++) {
                    if (last[j] < last[pos]) {
                        pos = j;
                    }
                }
            }
            frame[pos] = ref[i];
        }

        // Update time of last access
        last[pos] = i;

        // Display current frames state
        printf("Page %2d -> Frames: [", ref[i]);
        for (j = 0; j < f; j++) {
            if (frame[j] != -1)
                printf(" %d", frame[j]);
            else
                printf(" -");
        }
        printf(" ]\n");
    }

    printf("\nTotal Page Faults = %d\n", faults);
    printf("Press any key to exit...");
    getch();
}
