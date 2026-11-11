.PHONY: test test-local report clean

test:
	docker compose run --build --rm tests

test-local:
	dotnet test --solution Foundation.slnx

report:
	cmd.exe /c start artifacts\coverage-report\index.html

clean:
	rm -rf artifacts
