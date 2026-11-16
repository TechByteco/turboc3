

#include <stdio.h>

void decToBin(int n) {
    int bin[32], i = 0;
    if (n == 0) {
        printf("0");
        return;
    }
    while (n > 0) {
        bin[i++] = n % 2;
        n /= 2;
    }
    for (int j = i - 1; j >= 0; j--) {
        printf("%d", bin[j]);
    }
}

void decToHex(int n) {
    printf("%X", n);
}

void decToBCD(int n) {
    char digits[10];
    int i = 0;
    if (n == 0) {
        printf("0000");
        return;
    }
    while (n > 0) {
        digits[i++] = n % 10;
        n /= 10;
    }
    for (int j = i - 1; j >= 0; j--) {
        for (int b = 3; b >= 0; b--) {
            printf("%d", (digits[j] >> b) & 1);
        }
        printf(" ");
    }
}

int main() {
    int n;
    printf("Enter a decimal number: ");
    scanf("%d", &n);

    printf("Binary: ");
    decToBin(n);
    printf("\n");

    printf("Hex: ");
    decToHex(n);
    printf("\n");

    printf("BCD: ");
    decToBCD(n);
    printf("\n");

    return 0;
}

