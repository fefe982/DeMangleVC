#include <functional>
#include <utility>

struct A {
	A()
	: _cb{ []{} }
	{ }

	A(A&& src)
		: _cb(std::move(src._cb))
	{ }

	A& operator= (A&& src)
	{
		_cb = std::move(src._cb);
		return *this;
	}


	~A() {
		_cb();

	}

	std::function<void()> _cb;

};

void swap(A& lhs, A& rhs) {
	A temporary = std::move(lhs);
	lhs = std::move(rhs);
	rhs = std::move(temporary);

}

int main() {
	A x, y;
	swap(x, y);

}